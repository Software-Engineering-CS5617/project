using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;
using Chat;
using UI.Helpers;

namespace UI.ViewModels;

/// <summary>Presentation state for one chat message or thread reply.</summary>
public sealed class MessageViewModel : AvatarViewModel
{
    private static readonly Brush IncomingBrush = new SolidColorBrush(Color.FromArgb(255, 241, 244, 248));
    private static readonly Brush OutgoingBrush = new SolidColorBrush(Color.FromArgb(255, 220, 235, 255));
    private static readonly Brush DeletedBrush = new SolidColorBrush(Color.FromArgb(255, 243, 244, 246));
    private static readonly Brush MentionBorderBrush = new SolidColorBrush(Color.FromArgb(255, 224, 168, 62));
    private static readonly Brush MentionedBubbleBrush = new SolidColorBrush(Color.FromArgb(255, 255, 244, 214));
    private static readonly Brush MentionedOtherBubbleBrush = new SolidColorBrush(Color.FromArgb(255, 255, 249, 232));
    private static readonly Brush JumpHighlightBrush = new SolidColorBrush(Color.FromArgb(255, 255, 233, 168));
    private static readonly Brush JumpHighlightBorderBrush = new SolidColorBrush(Color.FromArgb(255, 217, 154, 0));
    private static readonly Brush NormalBorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    private readonly string _hostName;
    private readonly Action<MessageViewModel> _replyInChat;
    private readonly Action<MessageViewModel> _openThread;
    private readonly Action<MessageViewModel> _beginEdit;
    private readonly Action<MessageViewModel> _deleteMessage;
    private readonly Action<MessageViewModel, string> _toggleReaction;
    private readonly Action<Guid> _jumpToMessage;
    private string _content;
    private int _editCount;
    private string? _deletedByUser;
    private int _replyCount;
    private string _replyPreview = string.Empty;
    private string _customReactionText = string.Empty;
    private bool _isJumpHighlighted;
    private bool _isReactionPickerOpen;

    public MessageViewModel(Guid id, ChatMessage message, string currentUsername, string hostName,
        Action<MessageViewModel> replyInChat, Action<MessageViewModel> openThread,
        Action<MessageViewModel> beginEdit, Action<MessageViewModel> deleteMessage,
        Action<MessageViewModel, string> toggleReaction, Action<Guid> jumpToMessage,
        Guid? threadParentMessageId = null)
        : base(message is null ? "Unknown" : message.SenderName == ChatConstants.AiAssistantUsername ? ChatConstants.AiAssistantUsername : message.SenderName)
    {
        ArgumentNullException.ThrowIfNull(message);
        Id = id;
        SenderUsername = message.SenderName;
        ThreadParentMessageId = threadParentMessageId;
        ReplyToMessageId = message.ParentMessageId;
        Timestamp = message.Timestamp;
        Mentions = message.Mentions.Clone();
        CurrentUsername = currentUsername;
        _hostName = hostName;
        IsCurrentUserHost = string.Equals(currentUsername, hostName, StringComparison.Ordinal);
        _content = message.Content;
        _editCount = message.EditCount;
        _deletedByUser = message.DeletedByUser;
        _replyInChat = replyInChat;
        _openThread = openThread;
        _beginEdit = beginEdit;
        _deleteMessage = deleteMessage;
        _toggleReaction = toggleReaction;
        _jumpToMessage = jumpToMessage;

        if (message is ChatMessageWithThreads threadedMessage)
            _replyCount = threadedMessage.ThreadById.Count;

        foreach (var reaction in message.Reactions)
            Reactions.Add(new ReactionViewModel(reaction.Key, reaction.Value, currentUsername, emoji => _toggleReaction(this, emoji)));

        ReplyInChatCommand = new RelayCommand(() => _replyInChat(this), () => CanReplyInChat);
        OpenThreadCommand = new RelayCommand(() => _openThread(this), () => CanOpenThread);
        EditCommand = new RelayCommand(() => _beginEdit(this), () => CanEdit);
        DeleteCommand = new RelayCommand(() => _deleteMessage(this), () => CanDelete);
        ToggleReactionCommand = new RelayCommand(parameter =>
        {
            if (parameter is string emoji && !string.IsNullOrWhiteSpace(emoji))
                _toggleReaction(this, emoji);
        }, parameter => parameter is string emoji && !string.IsNullOrWhiteSpace(emoji) && !IsDeleted);
        JumpToReplyCommand = new RelayCommand(() =>
        {
            if (ReplyToMessageId is Guid targetId)
                _jumpToMessage(targetId);
        }, () => ReplyToMessageId is not null);
        SendCustomReactionCommand = new RelayCommand(SendCustomReaction, CanSendCustomReaction);
        ToggleReactionPickerCommand = new RelayCommand(
            () => IsReactionPickerOpen = !IsReactionPickerOpen, () => !IsDeleted);
    }

    public Guid Id { get; }
    public string SenderUsername { get; }
    public string SenderName => DisplayName;
    public string CurrentUsername { get; }
    public Guid? ThreadParentMessageId { get; }
    public Guid? ReplyToMessageId { get; }
    public DateTimeOffset Timestamp { get; }
    public string TimestampText => Timestamp.LocalDateTime.ToString("HH:mm");
    public bool IsOwnMessage => string.Equals(SenderUsername, CurrentUsername, StringComparison.Ordinal);
    public bool IsCurrentUserHost { get; }
    public HorizontalAlignment BubbleAlignment => IsOwnMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    public Brush BubbleBrush => IsDeleted ? DeletedBrush : IsJumpHighlighted ? JumpHighlightBrush
        : IsMentioned ? MentionedBubbleBrush
        : IsOwnMessage ? OutgoingBrush : IncomingBrush;
    public Brush MessageBorderBrush => IsJumpHighlighted ? JumpHighlightBorderBrush
        : IsMentioned ? MentionBorderBrush : NormalBorderBrush;
    public Thickness MessageBorderThickness => new(IsJumpHighlighted ? 3 : IsMentioned ? 2 : 0);
    public Brush ContentForeground => IsDeleted ? new SolidColorBrush(Color.FromArgb(255, 107, 114, 128)) : new SolidColorBrush(Color.FromArgb(255, 31, 41, 55));
    public FontStyle ContentFontStyle => IsDeleted ? FontStyle.Italic : FontStyle.Normal;
    public double MessageOpacity => IsDeleted ? 0.82 : 1.0;
    public Visibility LeftAvatarVisibility => IsOwnMessage ? Visibility.Collapsed : Visibility.Visible;
    public Visibility RightAvatarVisibility => IsOwnMessage ? Visibility.Visible : Visibility.Collapsed;

    public string Content => _content;
    public string DisplayContent => IsDeleted ? DeletedMessageText : Content;
    public int EditCount => _editCount;
    public bool IsEdited => _editCount > 0;
    public string EditedLabel => IsEdited ? $"edited · {_editCount}×" : string.Empty;
    public string? DeletedByUser => _deletedByUser;
    public bool IsDeleted => _deletedByUser is not null;
    public string DeletedMessageText => string.Equals(_deletedByUser, _hostName, StringComparison.Ordinal)
        ? "Message deleted by host" : $"Message deleted by {_deletedByUser}";

    public Mentions Mentions { get; }
    public bool HasMentions => Mentions.All || Mentions.Usernames.Count > 0;
    public bool IsMentioned => Mentions.IsUserMentioned(CurrentUsername);
    public string MentionLabel => !HasMentions ? string.Empty : IsMentioned ? "✦ YOU WERE MENTIONED"
        : Mentions.All ? "✦ Mentioned everyone"
        : $"To · {string.Join(", ", Mentions.Usernames.Select(name => name == ChatConstants.AiAssistantUsername ? ChatConstants.AiAssistantUsername : name))}";
    public Visibility MentionVisibility => HasMentions ? Visibility.Visible : Visibility.Collapsed;
    public string CustomReactionText
    {
        get => _customReactionText;
        set { if (SetProperty(ref _customReactionText, value)) SendCustomReactionCommand.NotifyCanExecuteChanged(); }
    }
    public bool IsJumpHighlighted => _isJumpHighlighted;
    public bool IsReactionPickerOpen
    {
        get => _isReactionPickerOpen;
        private set
        {
            if (SetProperty(ref _isReactionPickerOpen, value))
                OnPropertyChanged(nameof(ReactionPickerVisibility));
        }
    }
    public Visibility ReactionPickerVisibility => IsReactionPickerOpen && !IsDeleted
        ? Visibility.Visible : Visibility.Collapsed;

    public int ReplyCount => _replyCount;
    public string ReplyCountLabel => _replyCount == 1 ? "1 reply" : $"{_replyCount} replies";
    public bool HasReplies => _replyCount > 0;
    public Visibility ReplyCountVisibility => HasReplies ? Visibility.Visible : Visibility.Collapsed;
    public Visibility OpenThreadVisibility => CanOpenThread ? Visibility.Visible : Visibility.Collapsed;
    public bool CanOpenThread => ThreadParentMessageId is null;
    public bool CanReplyInChat => ThreadParentMessageId is null && !IsDeleted;
    public bool CanEdit => !IsDeleted && (IsOwnMessage || IsCurrentUserHost);
    public bool CanDelete => !IsDeleted && (IsOwnMessage || IsCurrentUserHost);
    public Visibility ReactionActionVisibility => IsDeleted ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ReplyInChatVisibility => CanReplyInChat ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MoreActionsVisibility => CanEdit || CanDelete ? Visibility.Visible : Visibility.Collapsed;

    public string ReplyPreviewText => string.IsNullOrEmpty(_replyPreview) ? string.Empty : _replyPreview;
    public Visibility ReplyPreviewVisibility => string.IsNullOrEmpty(_replyPreview) ? Visibility.Collapsed : Visibility.Visible;
    public ObservableCollection<ReactionViewModel> Reactions { get; } = new();
    public RelayCommand ReplyInChatCommand { get; }
    public RelayCommand OpenThreadCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand ToggleReactionCommand { get; }
    public RelayCommand JumpToReplyCommand { get; }
    public RelayCommand SendCustomReactionCommand { get; }
    public RelayCommand ToggleReactionPickerCommand { get; }

    private bool CanSendCustomReaction() => !IsDeleted && IsSingleEmoji(CustomReactionText);

    private void SendCustomReaction()
    {
        string emoji = CustomReactionText.Trim();
        if (!CanSendCustomReaction()) return;
        CustomReactionText = string.Empty;
        _toggleReaction(this, emoji);
    }

    private static bool IsSingleEmoji(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        string emoji = value.Trim();
        if (new StringInfo(emoji).LengthInTextElements != 1) return false;
        foreach (Rune rune in emoji.EnumerateRunes())
            if ((rune.Value >= 0x2300 && rune.Value <= 0x27FF) || rune.Value >= 0x1F000) return true;
        return false;
    }

    internal void SetJumpHighlighted(bool highlighted)
    {
        if (_isJumpHighlighted == highlighted) return;
        _isJumpHighlighted = highlighted;
        OnPropertyChanged(nameof(IsJumpHighlighted));
        OnPropertyChanged(nameof(BubbleBrush));
        OnPropertyChanged(nameof(MessageBorderBrush));
        OnPropertyChanged(nameof(MessageBorderThickness));
    }

    internal void SetReplyPreview(string sender, string content)
    {
        string preview = content.Length > 90 ? content[..87] + "..." : content;
        _replyPreview = $"↪ {sender}: {preview}";
        OnPropertyChanged(nameof(ReplyPreviewText));
        OnPropertyChanged(nameof(ReplyPreviewVisibility));
        JumpToReplyCommand.NotifyCanExecuteChanged();
    }

    internal void ApplyEdit(string newContent)
    {
        if (IsDeleted) return;
        _content = newContent;
        _editCount++;
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(DisplayContent));
        OnPropertyChanged(nameof(EditCount));
        OnPropertyChanged(nameof(IsEdited));
        OnPropertyChanged(nameof(EditedLabel));
    }

    internal void ApplyDeletion(string deleter)
    {
        if (IsDeleted) return;
        _deletedByUser = deleter;
        IsReactionPickerOpen = false;
        OnPropertyChanged(nameof(ReactionPickerVisibility));
        OnPropertyChanged(nameof(DeletedByUser));
        OnPropertyChanged(nameof(IsDeleted));
        OnPropertyChanged(nameof(DisplayContent));
        OnPropertyChanged(nameof(DeletedMessageText));
        OnPropertyChanged(nameof(ContentFontStyle));
        OnPropertyChanged(nameof(ContentForeground));
        OnPropertyChanged(nameof(MessageOpacity));
        OnPropertyChanged(nameof(BubbleBrush));
        OnPropertyChanged(nameof(CanReplyInChat));
        OnPropertyChanged(nameof(ReplyInChatVisibility));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(MoreActionsVisibility));
        OnPropertyChanged(nameof(ReplyPreviewText));
        ReplyInChatCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ToggleReactionCommand.NotifyCanExecuteChanged();
        ToggleReactionPickerCommand.NotifyCanExecuteChanged();
        SendCustomReactionCommand.NotifyCanExecuteChanged();
    }

    internal void ApplyReaction(string emoji, string username)
    {
        ReactionViewModel? reaction = Reactions.FirstOrDefault(item => item.Emoji == emoji);
        if (reaction is null)
        {
            Reactions.Add(new ReactionViewModel(emoji, new[] { username }, CurrentUsername, selected => _toggleReaction(this, selected)));
            return;
        }
        reaction.ToggleUser(username);
        if (reaction.Count == 0) Reactions.Remove(reaction);
    }

    internal void IncrementReplyCount()
    {
        _replyCount++;
        OnPropertyChanged(nameof(ReplyCount));
        OnPropertyChanged(nameof(HasReplies));
        OnPropertyChanged(nameof(ReplyCountLabel));
        OnPropertyChanged(nameof(ReplyCountVisibility));
    }
}
