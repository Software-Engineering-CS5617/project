using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Chat;
using UI.Helpers;
using UI.Services;

namespace UI.ViewModels;

/// <summary>Coordinates the chat panel and applies server updates without replacing the message list.</summary>
public sealed class ChatPanelViewModel : ObservableObject, IDisposable
{
    private const string AllMentionUsername = "@all";
    private readonly ChatClientAdapter _chat;
    private readonly Dictionary<Guid, MessageViewModel> _messagesById = new();
    private readonly Dictionary<Guid, List<MessageViewModel>> _threadMessagesByParentId = new();
    private readonly Dictionary<Guid, PendingRequest> _pendingRequests = new();
    private readonly Dictionary<string, ImageSource> _avatarSources = new(StringComparer.Ordinal);
    private static readonly TimeSpan RequestConfirmationTimeout = TimeSpan.FromSeconds(8);
    private bool _disposed, _initialized;
    private string _messageText = string.Empty, _threadMessageText = string.Empty, _statusMessage = string.Empty;
    private string _editText = string.Empty, _mentionSearchText = string.Empty;
    private MessageViewModel? _activeThreadRoot, _editingMessage, _replyingToMessage;

    public ChatPanelViewModel(ChatClientAdapter chat)
    {
        ArgumentNullException.ThrowIfNull(chat);
        _chat = chat;
        Messages = new();
        ThreadMessages = new();
        Participants = new();
        RebuildParticipants(_chat.Participants);
        MentionTargets = new();
        MentionSearchResults = new();
        SelectedMentions = new();
        UpdateMentionTargets();

        _chat.OperationApplied += OnOperationApplied;
        _chat.RequestCompleted += OnRequestCompleted;
        _chat.RoomReset += OnRoomReset;
        _chat.ParticipantsChanged += OnParticipantsChanged;

        SendMessageCommand = new RelayCommand(SendMessage, () => !string.IsNullOrWhiteSpace(MessageText));
        SendThreadReplyCommand = new RelayCommand(SendThreadReply, () => ActiveThreadRoot is not null && !string.IsNullOrWhiteSpace(ThreadMessageText));
        SyncCommand = new RelayCommand(Synchronize);
        ResetCommand = new RelayCommand(Reset);
        CloseThreadCommand = new RelayCommand(CloseThread);
        CancelEditCommand = new RelayCommand(CancelEdit);
        CancelChatReplyCommand = new RelayCommand(CancelChatReply);
        SaveEditCommand = new RelayCommand(SaveEdit, () => EditingMessage is { CanEdit: true } && !string.IsNullOrWhiteSpace(EditText));
    }

    public ObservableCollection<MessageViewModel> Messages { get; }
    public ObservableCollection<MessageViewModel> ThreadMessages { get; }
    public ObservableCollection<ParticipantViewModel> Participants { get; }
    public ObservableCollection<MentionTargetViewModel> MentionTargets { get; }
    public ObservableCollection<MentionTargetViewModel> MentionSearchResults { get; }
    public ObservableCollection<MentionTargetViewModel> SelectedMentions { get; }

    public string CurrentUsername => _chat.Username;
    public bool IsHost => _chat.IsHost;
    public int ParticipantCount => Participants.Count;
    public string ParticipantCountLabel => ParticipantCount == 1 ? "1 participant" : $"{ParticipantCount} participants";
    public event Action<Guid>? ScrollToMessageRequested;
    public string MessageText
    {
        get => _messageText;
        set { if (SetProperty(ref _messageText, value)) SendMessageCommand.NotifyCanExecuteChanged(); }
    }

    public string ThreadMessageText
    {
        get => _threadMessageText;
        set { if (SetProperty(ref _threadMessageText, value)) SendThreadReplyCommand.NotifyCanExecuteChanged(); }
    }

    public string MentionSearchText
    {
        get => _mentionSearchText;
        set { if (SetProperty(ref _mentionSearchText, value)) RefreshMentionSearchResults(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (SetProperty(ref _statusMessage, value)) OnPropertyChanged(nameof(StatusVisibility)); }
    }

    public Visibility StatusVisibility => string.IsNullOrWhiteSpace(StatusMessage) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility EmptyConversationVisibility => Messages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public MessageViewModel? ActiveThreadRoot
    {
        get => _activeThreadRoot;
        private set
        {
            if (SetProperty(ref _activeThreadRoot, value))
            {
                OnPropertyChanged(nameof(IsThreadOpen));
                OnPropertyChanged(nameof(ConversationVisibility));
                OnPropertyChanged(nameof(ThreadPanelVisibility));
                OnPropertyChanged(nameof(MainComposerVisibility));
                OnPropertyChanged(nameof(ThreadComposerVisibility));
                OnPropertyChanged(nameof(ThreadHeaderText));
            }
            SendThreadReplyCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsThreadOpen => ActiveThreadRoot is not null;
    public Visibility ConversationVisibility => IsThreadOpen ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ThreadPanelVisibility => IsThreadOpen ? Visibility.Visible : Visibility.Collapsed;
    public string ThreadHeaderText => ActiveThreadRoot is null ? "Thread" : $"Thread · {ActiveThreadRoot.SenderName}";

    public MessageViewModel? ReplyingToMessage
    {
        get => _replyingToMessage;
        private set
        {
            if (SetProperty(ref _replyingToMessage, value))
            {
                OnPropertyChanged(nameof(ReplyBannerVisibility));
                OnPropertyChanged(nameof(ReplyingToText));
            }
        }
    }

    public Visibility ReplyBannerVisibility => ReplyingToMessage is null ? Visibility.Collapsed : Visibility.Visible;
    public string ReplyingToText => ReplyingToMessage is null ? string.Empty : $"Replying to {ReplyingToMessage.SenderName}: {ReplyingToMessage.DisplayContent}";

    public MessageViewModel? EditingMessage
    {
        get => _editingMessage;
        private set
        {
            if (SetProperty(ref _editingMessage, value))
            {
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(MainComposerVisibility));
                OnPropertyChanged(nameof(ThreadComposerVisibility));
                OnPropertyChanged(nameof(EditComposerVisibility));
            }
            SaveEditCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsEditing => EditingMessage is not null;
    public string EditText
    {
        get => _editText;
        set { if (SetProperty(ref _editText, value)) SaveEditCommand.NotifyCanExecuteChanged(); }
    }

    public Visibility MainComposerVisibility => !IsEditing && !IsThreadOpen ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ThreadComposerVisibility => !IsEditing && IsThreadOpen ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EditComposerVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;

    public RelayCommand SendMessageCommand { get; }
    public RelayCommand SendThreadReplyCommand { get; }
    public RelayCommand SyncCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand CloseThreadCommand { get; }
    public RelayCommand SaveEditCommand { get; }
    public RelayCommand CancelEditCommand { get; }
    public RelayCommand CancelChatReplyCommand { get; }
    public void Initialize()
    {
        ThrowIfDisposed();
        if (_initialized) return;
        _initialized = true;
        Synchronize();
    }

    public void SetUserAvatar(string username, ImageSource? image)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        if (image is null) _avatarSources.Remove(username); else _avatarSources[username] = image;
        foreach (ParticipantViewModel participant in Participants.Where(p => p.Username == username)) participant.SetAvatarImage(image);
        foreach (MessageViewModel message in _messagesById.Values.Concat(_threadMessagesByParentId.Values.SelectMany(items => items)))
            if (message.SenderUsername == username) message.SetAvatarImage(image);
    }

    private void SendMessage()
    {
        string content = MessageText.Trim();
        if (content.Length == 0) return;
        Guid? replyToId = ReplyingToMessage?.Id;
        string[] mentioned = SelectedMentions.Select(item => item.Username).ToArray();
        Mentions mentions = BuildMentions();
        SubmitRequest("Send message", () => _chat.SendMessage(content, mentions, null, replyToId), () =>
        {
            if (MessageText == content) MessageText = string.Empty;
            if (ReplyingToMessage?.Id == replyToId) ReplyingToMessage = null;
            ClearMentions(mentioned);
        });
    }

    private void SendThreadReply()
    {
        if (ActiveThreadRoot is null) return;
        string content = ThreadMessageText.Trim();
        if (content.Length == 0) return;
        Guid parentId = ActiveThreadRoot.Id;
        string[] mentioned = SelectedMentions.Select(item => item.Username).ToArray();
        SubmitRequest("Send reply", () => _chat.SendMessage(content, BuildMentions(), parentId), () =>
        {
            if (ActiveThreadRoot?.Id == parentId && ThreadMessageText == content) ThreadMessageText = string.Empty;
            ClearMentions(mentioned);
        });
    }

    private Mentions BuildMentions()
    {
        Mentions mentions = new()
        {
            All = SelectedMentions.Any(target => target.Username == AllMentionUsername)
        };
        foreach (MentionTargetViewModel target in SelectedMentions.Where(target => target.Username != AllMentionUsername))
            mentions.Usernames.Add(target.Username);
        return mentions;
    }

    private void ClearMentions(IEnumerable<string> usernames)
    {
        foreach (string username in usernames.ToArray())
        {
            MentionTargetViewModel? target = MentionTargets.FirstOrDefault(item => item.Username == username);
            if (target is not null) RemoveMention(target);
        }
    }

    private void ToggleMention(MentionTargetViewModel target)
    {
        if (SelectedMentions.Remove(target)) target.SetSelected(false);
        else { SelectedMentions.Add(target); target.SetSelected(true); }
    }

    private void RemoveMention(MentionTargetViewModel target)
    {
        SelectedMentions.Remove(target);
        target.SetSelected(false);
    }

    private void UpdateMentionTargets()
    {
        string[] selected = SelectedMentions.Select(item => item.Username).ToArray();
        MentionTargets.Clear();
        foreach (string username in Participants.Select(item => item.Username).Where(name => name != CurrentUsername).Distinct(StringComparer.Ordinal))
            MentionTargets.Add(CreateMentionTarget(username, username == ChatConstants.AiAssistantUsername ? ChatConstants.AiAssistantUsername : username,
                username == ChatConstants.AiAssistantUsername ? "Virtual participant" : "Participant"));
        if (!MentionTargets.Any(item => item.Username == ChatConstants.AiAssistantUsername))
            MentionTargets.Add(CreateMentionTarget(ChatConstants.AiAssistantUsername, ChatConstants.AiAssistantUsername, "Virtual participant"));
        if (!MentionTargets.Any(item => item.Username == AllMentionUsername))
            MentionTargets.Add(CreateMentionTarget(AllMentionUsername, AllMentionUsername, "Everyone"));

        SelectedMentions.Clear();
        foreach (MentionTargetViewModel target in MentionTargets.Where(item => selected.Contains(item.Username, StringComparer.Ordinal)))
        {
            target.SetSelected(true);
            SelectedMentions.Add(target);
        }
        RefreshMentionSearchResults();
    }

    private MentionTargetViewModel CreateMentionTarget(string username, string displayName, string detail) =>
        new(username, displayName, detail, ToggleMention, RemoveMention);

    private void RefreshMentionSearchResults()
    {
        MentionSearchResults.Clear();
        IEnumerable<MentionTargetViewModel> matches = MentionTargets;
        if (!string.IsNullOrWhiteSpace(MentionSearchText))
            matches = matches.Where(item => item.DisplayName.Contains(MentionSearchText, StringComparison.OrdinalIgnoreCase)
                || item.Username.Contains(MentionSearchText, StringComparison.OrdinalIgnoreCase));
        foreach (MentionTargetViewModel target in matches) MentionSearchResults.Add(target);
    }

    private void Synchronize()
    {
        try { _chat.Sync(); StatusMessage = "Synchronizing chat history..."; }
        catch (Exception exception) { StatusMessage = $"Synchronization failed: {exception.Message}"; }
    }

    private void Reset()
    {
        try { _chat.Reset(); StatusMessage = "Requesting chat reset..."; }
        catch (Exception exception) { StatusMessage = $"Reset failed: {exception.Message}"; }
    }

    private void ReplyInChat(MessageViewModel message)
    {
        if (!message.CanReplyInChat) return;
        if (IsThreadOpen) CloseThread();
        ReplyingToMessage = message;
        StatusMessage = string.Empty;
    }

    private void CancelChatReply() => ReplyingToMessage = null;
    private void JumpToMessage(Guid messageId) => ScrollToMessageRequested?.Invoke(messageId);

    private void OpenThread(MessageViewModel message)
    {
        if (!message.CanOpenThread) return;
        if (IsEditing) CancelEdit();
        ReplyingToMessage = null;
        ActiveThreadRoot = message;
        ThreadMessages.Clear();
        if (_threadMessagesByParentId.TryGetValue(message.Id, out List<MessageViewModel>? replies))
            foreach (MessageViewModel reply in replies) ThreadMessages.Add(reply);
        ThreadMessageText = string.Empty;
        StatusMessage = string.Empty;
    }

    private void CloseThread()
    {
        if (IsEditing) CancelEdit();
        ActiveThreadRoot = null;
        ThreadMessages.Clear();
        ThreadMessageText = string.Empty;
    }

    private void BeginEdit(MessageViewModel message)
    {
        if (!message.CanEdit) return;
        EditingMessage = message;
        EditText = message.Content;
    }

    private void SaveEdit()
    {
        MessageViewModel? message = EditingMessage;
        if (message is null || !message.CanEdit) return;
        string content = EditText.Trim();
        if (content.Length == 0) return;
        SubmitRequest("Edit message", () => _chat.EditMessage(message.Id, content, message.ThreadParentMessageId), () =>
        {
            if (EditingMessage?.Id == message.Id) CancelEdit();
        });
    }

    private void CancelEdit()
    {
        EditingMessage = null;
        EditText = string.Empty;
    }

    private void DeleteMessage(MessageViewModel message)
    {
        if (message.CanDelete) SubmitRequest("Delete message", () => _chat.DeleteMessage(message.Id, message.ThreadParentMessageId));
    }

    private void ToggleReaction(MessageViewModel message, string emoji)
    {
        if (!message.IsDeleted && !string.IsNullOrWhiteSpace(emoji))
            SubmitRequest("React to message", () => _chat.ToggleReaction(message.Id, emoji, message.ThreadParentMessageId));
    }

    private void OnOperationApplied(ChatOperation operation)
    {
        if (_disposed) return;
        switch (operation)
        {
            case MessageAdded added: ApplyMessageAdded(added); break;
            case MessageEdited edited:
                MessageViewModel? editedMessage = FindMessage(edited.MessageId, edited.ThreadParentMessageId);
                editedMessage?.ApplyEdit(edited.NewContent);
                if (editedMessage is not null) RefreshReplyPreviews(editedMessage);
                break;
            case MessageDeleted deleted:
                MessageViewModel? deletedMessage = FindMessage(deleted.MessageId, deleted.ThreadParentMessageId);
                deletedMessage?.ApplyDeletion(deleted.ActorUsername);
                if (deletedMessage is not null) RefreshReplyPreviews(deletedMessage);
                break;
            case ReactionToggled reaction:
                FindMessage(reaction.MessageId, reaction.ThreadParentMessageId)?.ApplyReaction(reaction.Emoji, reaction.ActorUsername);
                break;
        }
    }

    private void ApplyMessageAdded(MessageAdded operation)
    {
        ChatMessageWithThreads message = operation.Message;
        if (operation.ThreadParentMessageId is Guid threadParentId)
        {
            AddThreadMessage(threadParentId, operation.MessageId, message, true);
            return;
        }
        if (_messagesById.ContainsKey(operation.MessageId)) return;
        MessageViewModel viewModel = CreateMessageViewModel(operation.MessageId, message);
        _messagesById.Add(operation.MessageId, viewModel);
        InsertSorted(Messages, viewModel);
        OnPropertyChanged(nameof(EmptyConversationVisibility));
        foreach (var reply in message.ThreadByTime)
            AddThreadMessage(operation.MessageId, reply.Key.Id, reply.Value, false);
    }

    private void AddThreadMessage(Guid parentId, Guid messageId, ChatMessage message, bool incrementRootCount)
    {
        if (!_threadMessagesByParentId.TryGetValue(parentId, out List<MessageViewModel>? replies))
        {
            replies = new();
            _threadMessagesByParentId.Add(parentId, replies);
        }
        if (replies.Any(reply => reply.Id == messageId)) return;
        MessageViewModel viewModel = CreateMessageViewModel(messageId, message, parentId);
        InsertSorted(replies, viewModel);
        if (incrementRootCount && _messagesById.TryGetValue(parentId, out MessageViewModel? root)) root.IncrementReplyCount();
        if (ActiveThreadRoot?.Id == parentId) InsertSorted(ThreadMessages, viewModel);
    }

    private MessageViewModel CreateMessageViewModel(Guid id, ChatMessage message, Guid? parentMessageId = null)
    {
        MessageViewModel viewModel = new(id, message, CurrentUsername, _chat.HostName,
            ReplyInChat, OpenThread, BeginEdit, DeleteMessage, ToggleReaction, JumpToMessage, parentMessageId);
        if (_avatarSources.TryGetValue(message.SenderName, out ImageSource? image)) viewModel.SetAvatarImage(image);
        if (parentMessageId is null
            && message.ParentMessageId is Guid replyId
            && _messagesById.TryGetValue(replyId, out MessageViewModel? target))
            SetReplyPreview(viewModel, target);
        return viewModel;
    }

    private static void SetReplyPreview(MessageViewModel reply, MessageViewModel target) =>
        reply.SetReplyPreview(target.SenderName, target.IsDeleted ? "Message deleted" : target.Content);

    private void RefreshReplyPreviews(MessageViewModel target)
    {
        foreach (MessageViewModel reply in Messages.Where(item => item.ReplyToMessageId == target.Id))
            SetReplyPreview(reply, target);
        if (ReplyingToMessage?.Id == target.Id)
            OnPropertyChanged(nameof(ReplyingToText));
    }

    private MessageViewModel? FindMessage(Guid id, Guid? parentId)
    {
        if (parentId is Guid rootId)
            return _threadMessagesByParentId.TryGetValue(rootId, out List<MessageViewModel>? replies) ? replies.FirstOrDefault(reply => reply.Id == id) : null;
        return _messagesById.TryGetValue(id, out MessageViewModel? message) ? message : null;
    }

    private ParticipantViewModel CreateParticipantViewModel(string username)
    {
        ParticipantViewModel participant = new(username, CurrentUsername, _chat.HostName);
        if (_avatarSources.TryGetValue(username, out ImageSource? image)) participant.SetAvatarImage(image);
        return participant;
    }

    private void RebuildParticipants(IReadOnlySet<string> participants)
    {
        Participants.Clear();
        foreach (string name in participants.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            Participants.Add(CreateParticipantViewModel(name));
        if (!Participants.Any(participant => participant.Username == ChatConstants.AiAssistantUsername))
            Participants.Add(CreateParticipantViewModel(ChatConstants.AiAssistantUsername));
        OnPropertyChanged(nameof(ParticipantCount));
        OnPropertyChanged(nameof(ParticipantCountLabel));
    }

    private void OnParticipantsChanged(IReadOnlySet<string> participants)
    {
        if (_disposed) return;
        RebuildParticipants(participants);
        UpdateMentionTargets();
        OnPropertyChanged(nameof(IsHost));
        if (StatusMessage.StartsWith("Synchronizing", StringComparison.OrdinalIgnoreCase)
            || StatusMessage.StartsWith("Connected", StringComparison.OrdinalIgnoreCase))
            StatusMessage = $"Connected · {ParticipantCount} {(ParticipantCount == 1 ? "participant" : "participants")}";
    }

    private void OnRoomReset()
    {
        if (_disposed) return;
        Messages.Clear();
        ThreadMessages.Clear();
        OnPropertyChanged(nameof(EmptyConversationVisibility));
        _messagesById.Clear();
        _threadMessagesByParentId.Clear();
        ActiveThreadRoot = null;
        ReplyingToMessage = null;
        CancelEdit();
        StatusMessage = "Chat history reset; waiting for synchronized history...";
    }

    private sealed record PendingRequest(string Description, Action? OnSuccess);

    private void SubmitRequest(string description, Func<Guid> request, Action? onSuccess = null)
    {
        ThrowIfDisposed();
        try
        {
            Guid id = request();
            _pendingRequests[id] = new PendingRequest(description, onSuccess);
            StatusMessage = $"{description} queued; waiting for server confirmation...";
            _ = WatchForRequestConfirmationAsync(id, description);
        }
        catch (Exception exception) { StatusMessage = $"{description} failed: {exception.Message}"; }
    }

    private async Task WatchForRequestConfirmationAsync(Guid requestId, string description)
    {
        await Task.Delay(RequestConfirmationTimeout).ConfigureAwait(false);
        _chat.TryDispatchToUI(() =>
        {
            if (!_disposed && _pendingRequests.ContainsKey(requestId))
                StatusMessage = $"No server confirmation for '{description}'. Check the connection and server Output window.";
        });
    }

    private void OnRequestCompleted(Guid requestId, RoomResult result)
    {
        if (_disposed || !_pendingRequests.Remove(requestId, out PendingRequest? pending)) return;
        switch (result)
        {
            case RoomResult.Ok:
                pending.OnSuccess?.Invoke();
                StatusMessage = $"{pending.Description} accepted by the server.";
                break;
            case RoomResult.NotFound: StatusMessage = "The requested message or thread was not found."; break;
            case RoomResult.Forbidden: StatusMessage = "You do not have permission to perform this action."; break;
            default: StatusMessage = $"{pending.Description} returned an unknown result."; break;
        }
    }

    private static void InsertSorted(IList<MessageViewModel> items, MessageViewModel item)
    {
        int index = 0;
        while (index < items.Count)
        {
            int comparison = items[index].Timestamp.CompareTo(item.Timestamp);
            if (comparison == 0) comparison = items[index].Id.CompareTo(item.Id);
            if (comparison > 0) break;
            index++;
        }
        items.Insert(index, item);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _chat.OperationApplied -= OnOperationApplied;
        _chat.RequestCompleted -= OnRequestCompleted;
        _chat.RoomReset -= OnRoomReset;
        _chat.ParticipantsChanged -= OnParticipantsChanged;
        _pendingRequests.Clear();
        _avatarSources.Clear();
    }
}
