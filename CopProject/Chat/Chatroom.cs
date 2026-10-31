namespace Chat;

/// <summary>
/// Stores the participants and messages for one chatroom.
/// </summary>
public class Chatroom : IChatroom
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<Guid, ChatMessageWithThreads> _byId = new();
    private readonly SortedDictionary<(DateTimeOffset Timestamp, Guid Id), ChatMessageWithThreads> _byTime = new();
    private readonly HashSet<string> _participants = new();

    /// <summary>
    /// Creates a chatroom and adds its host as the first participant.
    /// </summary>
    /// <param name="roomName">The room name.</param>
    /// <param name="hostName">The host's username.</param>
    public Chatroom(string roomName, string hostName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);

        RoomName = roomName;
        HostName = hostName;
        _participants.Add(hostName);
    }

    /// <summary>
    /// Gets or sets the room name.
    /// </summary>
    public string RoomName { get; set; }

    /// <summary>
    /// Gets or sets the host's username.
    /// </summary>
    public string HostName { get; set; }

    /// <summary>
    /// Gets a snapshot of the room participants.
    /// </summary>
    public IReadOnlySet<string> Participants
    {
        get
        {
            lock (_syncRoot)
            {
                return new HashSet<string>(_participants);
            }
        }
    }

    /// <inheritdoc />
    public bool AddParticipant(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_syncRoot)
        {
            return _participants.Add(name);
        }
    }

    /// <inheritdoc />
    public bool RemoveParticipant(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_syncRoot)
        {
            return _participants.Remove(name);
        }
    }

    /// <inheritdoc />
    public Guid AddMessage(ChatMessageWithThreads message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Guid id = Guid.NewGuid();
        AddMessage(id, message);
        return id;
    }

    /// <inheritdoc />
    public bool TryGetMessage(Guid id, out ChatMessage message)
    {
        lock (_syncRoot)
        {
            if (_byId.TryGetValue(id, out ChatMessageWithThreads? found))
            {
                message = found;
                return true;
            }

            foreach (ChatMessageWithThreads threadedMessage in _byId.Values)
            {
                if (threadedMessage.TryGetThreadMessage(id, out message!))
                {
                    return true;
                }
            }

            message = null!;
            return false;
        }
    }

    /// <inheritdoc />
    public RoomResult EditMessage(Guid id, string newContent, string editor, Guid? parentMessageId = null)
    {
        ArgumentNullException.ThrowIfNull(newContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(editor);

        lock (_syncRoot)
        {
            if (!TryGetTarget(id, parentMessageId, out ChatMessage? message))
            {
                return RoomResult.NotFound;
            }

            if (message.IsDeleted || !string.Equals(message.SenderName, editor, StringComparison.Ordinal))
            {
                return RoomResult.Forbidden;
            }

            message.Edit(newContent);
            return RoomResult.Ok;
        }
    }

    /// <inheritdoc />
    public RoomResult DeleteMessage(Guid id, string deleter, Guid? parentMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deleter);

        lock (_syncRoot)
        {
            if (!TryGetTarget(id, parentMessageId, out ChatMessage? message))
            {
                return RoomResult.NotFound;
            }

            if (!string.Equals(message.SenderName, deleter, StringComparison.Ordinal) || message.IsDeleted)
            {
                return RoomResult.Forbidden;
            }

            message.Delete(deleter);
            return RoomResult.Ok;
        }
    }

    /// <inheritdoc />
    public RoomResult ToggleReaction(Guid id, string emoji, string user, Guid? parentMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);
        ArgumentException.ThrowIfNullOrWhiteSpace(user);

        lock (_syncRoot)
        {
            if (!TryGetTarget(id, parentMessageId, out ChatMessage? message))
            {
                return RoomResult.NotFound;
            }

            if (message.IsDeleted)
            {
                return RoomResult.Forbidden;
            }

            message.ToggleReaction(emoji, user);
            return RoomResult.Ok;
        }
    }

    /// <summary>
    /// Adds a reply to the specified message thread.
    /// </summary>
    /// <param name="parentMessageId">The thread's root message ID.</param>
    /// <param name="reply">The reply to add.</param>
    /// <returns>The new reply's ID.</returns>
    public Guid AddThreadMessage(Guid parentMessageId, ChatMessage reply)
    {
        ArgumentNullException.ThrowIfNull(reply);

        lock (_syncRoot)
        {
            if (!_byId.TryGetValue(parentMessageId, out ChatMessageWithThreads? parent))
            {
                throw new KeyNotFoundException("The parent message was not found.");
            }

            Guid id = Guid.NewGuid();
            parent.AddMessageToThread(id, reply);
            return id;
        }
    }

    /// <summary>
    /// Adds a message with its assigned ID.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="message">The message to add.</param>
    internal void AddMessage(Guid id, ChatMessageWithThreads message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_syncRoot)
        {
            if (_byId.ContainsKey(id))
            {
                throw new ArgumentException("A message with this ID already exists.", nameof(id));
            }

            _byId.Add(id, message);
            _byTime.Add((message.Timestamp, id), message);
        }
    }

    /// <summary>
    /// Replaces the room's participant list.
    /// </summary>
    /// <param name="participants">The participants to set.</param>
    internal void SetParticipants(IEnumerable<string> participants)
    {
        ArgumentNullException.ThrowIfNull(participants);

        lock (_syncRoot)
        {
            _participants.Clear();
            _participants.UnionWith(participants);
        }
    }

    /// <summary>
    /// Removes all room messages.
    /// </summary>
    internal void ClearMessages()
    {
        lock (_syncRoot)
        {
            _byId.Clear();
            _byTime.Clear();
        }
    }

    /// <summary>
    /// Adds a reply to a thread using its existing ID.
    /// </summary>
    /// <param name="parentMessageId">The thread's root message ID.</param>
    /// <param name="messageId">The reply's assigned ID.</param>
    /// <param name="reply">The reply to add.</param>
    /// <returns>The result of adding the reply.</returns>
    internal RoomResult AddThreadMessageById(Guid parentMessageId, Guid messageId, ChatMessage reply)
    {
        lock (_syncRoot)
        {
            if (!_byId.TryGetValue(parentMessageId, out ChatMessageWithThreads? parent))
            {
                return RoomResult.NotFound;
            }

            parent.AddMessageToThread(messageId, reply);
            return RoomResult.Ok;
        }
    }

    /// <summary>
    /// Creates a clone of a room or thread message.
    /// </summary>
    /// <param name="message">The message to clone.</param>
    /// <returns>A clone of the message.</returns>
    internal static ChatMessage CloneMessage(ChatMessage message)
    {
        if (message is ChatMessageWithThreads threadedMessage)
        {
            return threadedMessage.DeepClone();
        }

        ChatMessage clone = new()
        {
            Timestamp = message.Timestamp,
            SenderName = message.SenderName,
            Mentions = ChatMessageWithThreads.CloneMentions(message.Mentions),
            Content = message.Content
        };
        clone.CopyMutableStateFrom(message);
        return clone;
    }

    /// <summary>
    /// Finds a room message or a message within the specified thread.
    /// </summary>
    /// <param name="id">The target message ID.</param>
    /// <param name="parentMessageId">The thread root ID, or <see langword="null"/> for a room message.</param>
    /// <param name="message">The found message, if any.</param>
    /// <returns><see langword="true"/> if the message was found.</returns>
    private bool TryGetTarget(Guid id, Guid? parentMessageId, out ChatMessage? message)
    {
        if (parentMessageId is Guid parentId)
        {
            if (_byId.TryGetValue(parentId, out ChatMessageWithThreads? parent)
                && parent.TryGetThreadMessage(id, out ChatMessage threadMessage))
            {
                message = threadMessage;
                return true;
            }

            message = null;
            return false;
        }

        if (_byId.TryGetValue(id, out ChatMessageWithThreads? roomMessage))
        {
            message = roomMessage;
            return true;
        }

        message = null;
        return false;
    }
}
