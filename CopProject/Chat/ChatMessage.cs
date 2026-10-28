namespace Chat;

/// <summary>
/// Represents a message sent in a chatroom or thread.
/// </summary>
/// <remarks>
/// These messages cannot be parents of threads.
/// </remarks>
public class ChatMessage : IChatMessage
{
    private readonly object _syncRoot = new();

    /// <summary>
    /// Gets the time at which the message was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the name of the message sender.
    /// </summary>
    public required string SenderName { get; init; }

    /// <summary>
    /// Gets the mentions attached to this message.
    /// </summary>
    public Mentions Mentions { get; init; } = new();

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets the number of times this message has been edited.
    /// </summary>
    public int EditCount { get; private set; }

    /// <summary>
    /// Gets the username of the user who deleted the message, if any.
    /// </summary>
    public string? DeletedByUser { get; private set; }

    /// <summary>
    /// Gets or sets the ID of the message this message replies to, if any.
    /// </summary>
    public Guid? ParentMessageId;

    /// <summary>
    /// Gets the emoji reactions and the users who used each reaction.
    /// </summary>
    public Dictionary<string, HashSet<string>> Reactions { get; } = new();

    /// <inheritdoc />
    public bool IsDeleted
    {
        get
        {
            lock (_syncRoot)
            {
                return DeletedByUser is not null;
            }
        }
    }

    /// <inheritdoc />
    public void Edit(string newContent)
    {
        ArgumentNullException.ThrowIfNull(newContent);

        lock (_syncRoot)
        {
            if (DeletedByUser is not null)
            {
                throw new InvalidOperationException("A deleted message cannot be edited.");
            }

            Content = newContent;
            EditCount++;
        }
    }

    /// <inheritdoc />
    public void Delete(string deleter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deleter);

        lock (_syncRoot)
        {
            if (DeletedByUser is not null)
            {
                throw new InvalidOperationException("A deleted message cannot be deleted again.");
            }

            DeletedByUser = deleter;
        }
    }

    /// <inheritdoc />
    public bool ToggleReaction(string emoji, string user)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);
        ArgumentException.ThrowIfNullOrWhiteSpace(user);

        lock (_syncRoot)
        {
            if (DeletedByUser is not null)
            {
                throw new InvalidOperationException("A deleted message cannot have its reactions changed.");
            }

            if (Reactions.TryGetValue(emoji, out HashSet<string>? users) && users.Remove(user))
            {
                if (users.Count == 0)
                {
                    Reactions.Remove(emoji);
                }

                return false;
            }

            if (users is null)
            {
                users = new HashSet<string>();
                Reactions.Add(emoji, users);
            }

            users.Add(user);
            return true;
        }
    }
}
