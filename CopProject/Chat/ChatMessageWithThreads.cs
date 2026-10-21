namespace Chat;

/// <summary>
/// Represents a message that can own a single-level thread of replies.
/// </summary>
public class ChatMessageWithThreads : ChatMessage, IChatMessageWithThreads
{
    private readonly object _threadSyncRoot = new();

    /// <summary>
    /// Gets the thread messages indexed by their IDs.
    /// </summary>
    public readonly Dictionary<Guid, ChatMessage> ThreadById = new();

    /// <summary>
    /// Gets the thread messages ordered by timestamp and ID.
    /// </summary>
    public readonly SortedDictionary<(DateTimeOffset Timestamp, Guid Id), ChatMessage> ThreadByTime = new();

    /// <inheritdoc />
    public void AddMessageToThread(ChatMessage reply)
    {
        ArgumentNullException.ThrowIfNull(reply);
        AddMessageToThread(Guid.NewGuid(), reply);
    }

    internal void AddMessageToThread(Guid id, ChatMessage reply)
    {
        ArgumentNullException.ThrowIfNull(reply);

        lock (_threadSyncRoot)
        {
            if (ThreadById.ContainsKey(id))
            {
                throw new ArgumentException("A thread message with this ID already exists.", nameof(id));
            }

            ThreadById.Add(id, reply);
            ThreadByTime.Add((reply.Timestamp, id), reply);
        }
    }

    internal bool TryGetThreadMessage(Guid id, out ChatMessage message)
    {
        lock (_threadSyncRoot)
        {
            return ThreadById.TryGetValue(id, out message!);
        }
    }
}
