using System.Net;
using System.Text.Json;
using Controller;
using Networking;
using Utils;

namespace Chat;

/// <summary>
/// Owns the authoritative chatroom state and synchronizes connected clients.
/// </summary>
public sealed class ChatServer : IMessageListener, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly ICommunicator _communicator;
    private readonly List<ChatOperation> _operations = new();
    private readonly Dictionary<string, int> _clientVersions = new(StringComparer.Ordinal);
    private readonly IControllerService _controller;
    private readonly Dictionary<string, ControllerUser> _controllerUsers = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>
    /// Creates a chat server.
    /// </summary>
    /// <param name="chatroom">The server's chatroom copy.</param>
    /// <param name="controller">The controller.</param>
    public ChatServer(Chatroom chatroom, IControllerService controller)
    {
        ArgumentNullException.ThrowIfNull(chatroom);
        ArgumentNullException.ThrowIfNull(controller);

        ICommunicator communicator = controller.Communicator
            ?? throw new InvalidOperationException("The controller does not have an active communicator.");
        ControllerUser currentUser = controller.CurrentUser
            ?? throw new InvalidOperationException("The controller does not have a current user.");

        Chatroom = chatroom;
        _communicator = communicator;
        _controller = controller;
        _communicator.AddSubscriber(ChatConstants.ModuleId, this);
        _controller.UserJoined += OnControllerUserJoined;
        _controller.UserLeft += OnControllerUserLeft;

        foreach (ControllerUser user in _controller.GetActiveUsers())
        {
            if (!string.Equals(user.Username, currentUser.Username, StringComparison.Ordinal))
            {
                _controllerUsers[user.Username] = user;
                RegisterClient(user.Username);
            }
        }
    }

    /// <summary>
    /// Raised when the server cannot deliver a synchronization batch.
    /// </summary>
    public event Action<string>? DeliveryFailed;

    /// <summary>
    /// Raised after an operation is accepted and applied to the authoritative room.
    /// </summary>
    public event Action<ChatOperation>? OperationApplied;

    /// <summary>
    /// Gets the authoritative chatroom.
    /// </summary>
    public Chatroom Chatroom { get; }

    /// <summary>
    /// Gets the number of successfully accepted general operations.
    /// </summary>
    public int Version
    {
        get
        {
            lock (_syncRoot)
            {
                return _operations.Count;
            }
        }
    }

    /// <summary>
    /// Registers a participant so the server can track their synchronization state.
    /// </summary>
    /// <param name="username">The participant's username.</param>
    /// <returns><see langword="true"/> if the client was newly registered.</returns>
    public bool RegisterClient(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        lock (_syncRoot)
        {
            if (_clientVersions.ContainsKey(username))
            {
                return false;
            }

            Chatroom.AddParticipant(username);
            _clientVersions.Add(username, 0);
            SendBatch(username, 0);
            BroadcastParticipants();
            return true;
        }
    }

    /// <summary>
    /// Unregisters a participant and removes them from the room.
    /// </summary>
    /// <param name="username">The participant's username.</param>
    /// <returns><see langword="true"/> if the client was registered.</returns>
    public bool UnregisterClient(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        lock (_syncRoot)
        {
            if (!_clientVersions.Remove(username))
            {
                return false;
            }

            if (!string.Equals(username, Chatroom.HostName, StringComparison.Ordinal))
            {
                Chatroom.RemoveParticipant(username);
            }

            BroadcastParticipants();
            return true;
        }
    }

    /// <summary>
    /// Processes a request associated with the sender's asserted username.
    /// </summary>
    /// <param name="request">The request envelope.</param>
    public void HandleRequest(ChatClientRequestEnvelope request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Username);

        string username = request.Username;
        lock (_syncRoot)
        {
            if (!_clientVersions.TryGetValue(username, out int clientVersion))
            {
                Send(username, new ChatServerRequestResult(request.RequestId, RoomResult.Forbidden));
                return;
            }

            switch (request.Request)
            {
                case Sync:
                    SendBatch(username, clientVersion);
                    break;

                case Reset:
                    SendUpdate(username, new ChatServerReset());
                    _clientVersions[username] = 0;
                    SendBatch(username, 0);
                    break;

                case ChatRoomChangeRequest changeRequest:
                    HandleOperation(username, request.RequestId, changeRequest);
                    break;

                default:
                    Send(username, new ChatServerRequestResult(request.RequestId, RoomResult.Forbidden));
                    break;
            }
        }
    }

    /// <summary>
    /// Unsubscribes the server from communicator messages.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _communicator.RemoveSubscriber(ChatConstants.ModuleId);
        _controller.UserJoined -= OnControllerUserJoined;
        _controller.UserLeft -= OnControllerUserLeft;

        _disposed = true;
    }

    /// <summary>
    /// Applies an operation initiated locally by the host and synchronizes remote clients.
    /// </summary>
    /// <param name="request">The host's chat operation.</param>
    /// <returns>The result of applying the operation.</returns>
    public RoomResult SubmitHostRequest(ChatRoomChangeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_syncRoot)
        {
            RoomResult result = ApplyRoomChange(Chatroom.HostName, request, out ChatOperation? operation);
            if (result == RoomResult.Ok)
            {
                RecordOperation(operation!);
            }

            return result;
        }
    }

    /// <inheritdoc />
    public void OnMessageReceived(byte[] message)
    {
        ArgumentNullException.ThrowIfNull(message);

        ChatClientRequestEnvelope? request = JsonSerializer.Deserialize<ChatClientRequestEnvelope>(message);
        if (request is null)
        {
            throw new InvalidDataException("The received chat request was empty.");
        }

        HandleRequest(request);
    }

    /// <summary>
    /// Registers a participant reported by the Controller.
    /// </summary>
    /// <param name="sender">The Controller raising the event.</param>
    /// <param name="e">The participant who joined.</param>
    private void OnControllerUserJoined(object? sender, UserJoinedEventArgs e)
    {
        lock (_syncRoot)
        {
            _controllerUsers[e.User.Username] = e.User;
        }

        RegisterClient(e.User.Username);
    }

    /// <summary>
    /// Unregisters a participant reported as leaving by the Controller.
    /// </summary>
    /// <param name="sender">The Controller raising the event.</param>
    /// <param name="e">The participant who left.</param>
    private void OnControllerUserLeft(object? sender, UserLeftEventArgs e)
    {
        lock (_syncRoot)
        {
            _controllerUsers.Remove(e.User.Username);
        }

        UnregisterClient(e.User.Username);
    }

    /// <summary>
    /// Applies a client's room change and sends the result.
    /// </summary>
    /// <param name="username">The requesting participant's username.</param>
    /// <param name="requestId">The ID of the client's request.</param>
    /// <param name="request">The requested room change.</param>
    private void HandleOperation(string username, Guid requestId, ChatRoomChangeRequest request)
    {
        RoomResult result = ApplyRoomChange(username, request, out ChatOperation? operation);
        Send(username, new ChatServerRequestResult(requestId, result));
        if (result != RoomResult.Ok)
        {
            return;
        }

        RecordOperation(operation!);
    }

    /// <summary>
    /// Applies a room-changing request to the authoritative chatroom.
    /// </summary>
    /// <param name="username">The participant performing the request.</param>
    /// <param name="request">The room change to apply.</param>
    /// <param name="operation">The operation created when the request succeeds.</param>
    /// <returns>The result of applying the room change.</returns>
    private RoomResult ApplyRoomChange(string username, ChatRoomChangeRequest request, out ChatOperation? operation)
    {
        operation = null;

        switch (request)
        {
            case SendMessage send:
            {
                ArgumentNullException.ThrowIfNull(send.Mentions);
                ChatMessageWithThreads serverMessage = new()
                {
                    Timestamp = send.Timestamp,
                    SenderName = username,
                    Mentions = send.Mentions.Clone(),
                    Content = send.Content
                };
                serverMessage.ParentMessageId = send.ReplyToMessageId;
                Guid messageId;
                if (send.ThreadParentMessageId is Guid parentId)
                {
                    try
                    {
                        messageId = Chatroom.AddThreadMessage(parentId, serverMessage);
                    }
                    catch (KeyNotFoundException)
                    {
                        return RoomResult.NotFound;
                    }
                }
                else
                {
                    messageId = Chatroom.AddMessage(serverMessage);
                }

                operation = new MessageAdded(
                    messageId,
                    serverMessage.DeepCloneWithoutThreadMessages(),
                    send.ThreadParentMessageId);
                return RoomResult.Ok;
            }

            case EditMessage edit:
            {
                RoomResult result = Chatroom.EditMessage(
                    edit.MessageId,
                    edit.NewContent,
                    username,
                    edit.ThreadParentMessageId);
                if (result == RoomResult.Ok)
                {
                    operation = new MessageEdited(edit.MessageId, edit.NewContent, username, edit.ThreadParentMessageId);
                }

                return result;
            }

            case DeleteMessage delete:
            {
                RoomResult result = Chatroom.DeleteMessage(delete.MessageId, username, delete.ThreadParentMessageId);
                if (result == RoomResult.Ok)
                {
                    operation = new MessageDeleted(delete.MessageId, username, delete.ThreadParentMessageId);
                }

                return result;
            }

            case ToggleReaction reaction:
            {
                RoomResult result = Chatroom.ToggleReaction(
                    reaction.MessageId,
                    reaction.Emoji,
                    username,
                    reaction.ThreadParentMessageId);
                if (result == RoomResult.Ok)
                {
                    operation = new ReactionToggled(
                        reaction.MessageId,
                        reaction.Emoji,
                        username,
                        reaction.ThreadParentMessageId);
                }

                return result;
            }

            default:
                return RoomResult.Forbidden;
        }
    }

    /// <summary>
    /// Records an accepted operation and synchronizes clients.
    /// </summary>
    /// <param name="operation">The accepted operation.</param>
    private void RecordOperation(ChatOperation operation)
    {
        _operations.Add(operation);
        OperationApplied?.Invoke(operation);
        foreach ((string recipient, int version) in _clientVersions.ToArray())
        {
            if (version != _operations.Count)
            {
                SendBatch(recipient, version);
            }
        }
    }

    /// <summary>
    /// Sends all operations after the specified client version.
    /// </summary>
    /// <param name="username">The recipient's username.</param>
    /// <param name="fromVersion">The version the recipient has already applied.</param>
    private void SendBatch(string username, int fromVersion)
    {
        ChatOperation[] operations = _operations.Skip(fromVersion).ToArray();
        ChatServerOperationBatch batch = new(fromVersion, _operations.Count, operations);
        if (SendUpdate(username, batch))
        {
            _clientVersions[username] = _operations.Count;
        }
    }

    /// <summary>
    /// Sends an update with the current participant list.
    /// </summary>
    /// <param name="username">The recipient's username.</param>
    /// <param name="update">The update to send.</param>
    /// <returns><see langword="true"/> if delivery succeeds.</returns>
    private bool SendUpdate(string username, ChatServerUpdate update)
    {
        ChatServerUpdateEnvelope envelope = new(
            update,
            Chatroom.Participants.Order(StringComparer.Ordinal).ToArray());
        return Send(username, envelope);
    }

    /// <summary>
    /// Serializes and sends a message to a participant.
    /// </summary>
    /// <param name="username">The recipient's username.</param>
    /// <param name="message">The message to send.</param>
    /// <returns><see langword="true"/> if the message is sent.</returns>
    private bool Send(string username, ChatServerMessage message)
    {
        if (!_clientVersions.ContainsKey(username))
        {
            DeliveryFailed?.Invoke(username);
            return false;
        }

        IPEndPoint endPoint;
        if (!_controllerUsers.TryGetValue(username, out ControllerUser? user))
        {
            user = _controller.GetActiveUsers()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.Username, username, StringComparison.Ordinal));
            if (user is not null)
            {
                // Refresh the cache if the Controller had not reported this user yet.
                _controllerUsers[username] = user;
            }
        }

        if (user is not null
            && IPAddress.TryParse(user.Ip, out IPAddress? ipAddress)
            && user.Port is >= 1 and <= 65535)
        {
            endPoint = new IPEndPoint(ipAddress, user.Port);
        }
        else
        {
            DeliveryFailed?.Invoke(username);
            return false;
        }

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message);
        _communicator.SendMessage(endPoint, payload, ChatConstants.ModuleId, priority: 1);
        return true;
    }

    /// <summary>
    /// Sends the current participant list to every registered client.
    /// </summary>
    private void BroadcastParticipants()
    {
        IReadOnlyList<string> participants = Chatroom.Participants.Order(StringComparer.Ordinal).ToArray();
        foreach ((string username, int version) in _clientVersions)
        {
            ChatServerOperationBatch update = new(version, version, Array.Empty<ChatOperation>());
            Send(username, new ChatServerUpdateEnvelope(update, participants));
        }
    }
}
