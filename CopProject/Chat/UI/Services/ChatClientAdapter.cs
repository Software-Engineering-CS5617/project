using Chat;
using Controller;
using Microsoft.UI.Dispatching;

namespace UI.Services;

/// <summary>
/// Connects the Chat backend to the UI layer.
/// Handles backend events and dispatches notifications to the UI thread.
/// </summary>
public sealed class ChatClientAdapter : IDisposable
{
    private readonly ChatClient? _client;
    private readonly ChatServer? _server;
    private readonly IControllerService? _controller;
    private readonly DispatcherQueue _dispatcherQueue;

    private int _disposed;

    public ChatClientAdapter(
        ChatClient client,
        DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        _client = client;
        _dispatcherQueue = dispatcherQueue;

        // Subscribe to backend events.
        _client.OperationApplied += OnOperationApplied;
        _client.RequestCompleted += OnRequestCompleted;
        _client.RoomReset += OnRoomReset;
        _client.ParticipantsChanged += OnParticipantsChanged;
    }

    /// <summary>
    /// Creates an adapter for the host, whose chat operations are applied
    /// directly by the authoritative <see cref="ChatServer"/>.
    /// </summary>
    public ChatClientAdapter(
        ChatServer server,
        IControllerService controller,
        DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        _server = server;
        _controller = controller;
        _dispatcherQueue = dispatcherQueue;

        _server.OperationApplied += OnOperationApplied;
        _controller.UserJoined += OnControllerUserJoined;
        _controller.UserLeft += OnControllerUserLeft;
    }

    // Initial information from the connected chatroom.

    public string Username => _client?.Username ?? _server!.Chatroom.HostName;

    public string HostName => _client?.Chatroom.HostName ?? _server!.Chatroom.HostName;

    public bool IsHost => _server is not null || string.Equals(Username, HostName, StringComparison.Ordinal);

    public IReadOnlySet<string> Participants =>
        _client?.Chatroom.Participants ?? _server!.Chatroom.Participants;

    // Events exposed to the ViewModels.
    // These events are always raised on the UI thread.

    public event Action<ChatOperation>? OperationApplied;

    public event Action<Guid, RoomResult>? RequestCompleted;

    public event Action? RoomReset;

    public event Action<IReadOnlySet<string>>? ParticipantsChanged;

    // Backend event handlers.

    private void OnOperationApplied(ChatOperation operation)
    {
        DispatchToUI(() => {
            OperationApplied?.Invoke(operation);
        });
    }

    private void OnRequestCompleted(Guid requestId, RoomResult result)
    {
        DispatchToUI(() => {
            RequestCompleted?.Invoke(requestId, result);
        });
    }

    private void OnRoomReset()
    {
        DispatchToUI(() => {
            RoomReset?.Invoke();
        });
    }

    private void OnParticipantsChanged(IReadOnlySet<string> participants)
    {
        // Create an independent snapshot before dispatching.
        var snapshot = new HashSet<string>(participants);

        DispatchToUI(() => {
            ParticipantsChanged?.Invoke(snapshot);
        });
    }

    private void OnControllerUserJoined(object? sender, UserJoinedEventArgs e) => RaiseHostParticipantsChanged();

    private void OnControllerUserLeft(object? sender, UserLeftEventArgs e) => RaiseHostParticipantsChanged();

    private void RaiseHostParticipantsChanged()
    {
        if (_server is null)
            return;

        var snapshot = new HashSet<string>(_server.Chatroom.Participants);

        // Queue this so ChatServer has already applied the participant change.
        _dispatcherQueue.TryEnqueue(() => {
            if (Volatile.Read(ref _disposed) == 0)
                ParticipantsChanged?.Invoke(snapshot);
        });
    }

    // Dispatcher implementation.

    /// <summary>
    /// Schedules work on the UI thread. The callback executes immediately when
    /// called from the UI thread, otherwise it is queued through DispatcherQueue.
    /// </summary>
    public bool TryDispatchToUI(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (Volatile.Read(ref _disposed) != 0)
            return false;

        if (_dispatcherQueue.HasThreadAccess)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return false;

            callback();
            return true;
        }

        return _dispatcherQueue.TryEnqueue(() =>
        {
            // The adapter might have been disposed while the callback was queued.
            if (Volatile.Read(ref _disposed) == 0)
                callback();
        });
    }

    private void DispatchToUI(Action callback)
    {
        if (!TryDispatchToUI(callback))
        {
            System.Diagnostics.Trace.WriteLine(
                $"[ChatClientAdapter:{Username}] The UI dispatcher is unavailable; an update was not applied.");
        }
    }

    // Chat operations exposed to the ViewModels.

    public Guid Sync()
    {
        ThrowIfDisposed();

        if (_server is not null)
        {
            RaiseHostParticipantsChanged();
            return Guid.NewGuid();
        }

        return _client!.Sync();
    }

    public Guid Reset()
    {
        ThrowIfDisposed();

        if (_server is not null)
            return Guid.NewGuid();

        return _client!.Reset();
    }

    public Guid SendMessage(
        string content,
        Mentions mentions,
        Guid? threadParentMessageId = null,
        Guid? replyToMessageId = null)
    {
        ThrowIfDisposed();

        if (_server is not null)
        {
            return SubmitHostRequest(new SendMessage(
                DateTimeOffset.UtcNow,
                mentions.Clone(),
                content,
                threadParentMessageId,
                replyToMessageId));
        }

        return _client!.SendMessage(
            content,
            mentions,
            threadParentMessageId,
            replyToMessageId);
    }

    public Guid EditMessage(
        Guid messageId,
        string newContent,
        Guid? threadParentMessageId = null)
    {
        ThrowIfDisposed();

        if (_server is not null)
            return SubmitHostRequest(new EditMessage(messageId, newContent, threadParentMessageId));

        return _client!.EditMessage(
            messageId,
            newContent,
            threadParentMessageId);
    }

    public Guid DeleteMessage(
        Guid messageId,
        Guid? threadParentMessageId = null)
    {
        ThrowIfDisposed();

        if (_server is not null)
            return SubmitHostRequest(new DeleteMessage(messageId, threadParentMessageId));

        return _client!.DeleteMessage(
            messageId,
            threadParentMessageId);
    }

    public Guid ToggleReaction(
        Guid messageId,
        string emoji,
        Guid? threadParentMessageId = null)
    {
        ThrowIfDisposed();

        if (_server is not null)
            return SubmitHostRequest(new global::Chat.ToggleReaction(messageId, emoji, threadParentMessageId));

        return _client!.ToggleReaction(
            messageId,
            emoji,
            threadParentMessageId);
    }

    private Guid SubmitHostRequest(ChatRoomChangeRequest request)
    {
        Guid requestId = Guid.NewGuid();
        RoomResult result = _server!.SubmitHostRequest(request);

        // Defer the completion so ChatPanelViewModel has registered its pending request.
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (Volatile.Read(ref _disposed) == 0)
                RequestCompleted?.Invoke(requestId, result);
        });

        return requestId;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposed) != 0,
            this);
    }

    // Unsubscribe when the adapter is no longer needed.

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (_client is not null)
        {
            _client.OperationApplied -= OnOperationApplied;
            _client.RequestCompleted -= OnRequestCompleted;
            _client.RoomReset -= OnRoomReset;
            _client.ParticipantsChanged -= OnParticipantsChanged;
        }

        if (_server is not null)
            _server.OperationApplied -= OnOperationApplied;

        if (_controller is not null)
        {
            _controller.UserJoined -= OnControllerUserJoined;
            _controller.UserLeft -= OnControllerUserLeft;
        }
    }
}
