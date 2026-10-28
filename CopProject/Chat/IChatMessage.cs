namespace Chat;

/// <summary>
/// Describes the operations supported by a chat message.
/// </summary>
public interface IChatMessage
{
    /// <summary>
    /// Gets whether this message has been deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Replaces the message content and records an edit.
    /// </summary>
    /// <param name="newContent">The replacement content.</param>
    void Edit(string newContent);

    /// <summary>
    /// Marks the message as deleted by a user.
    /// </summary>
    /// <param name="deleter">The username of the user deleting the message.</param>
    void Delete(string deleter);

    /// <summary>
    /// Adds or removes a user's reaction from the message.
    /// </summary>
    /// <param name="emoji">The reaction emoji.</param>
    /// <param name="user">The username whose reaction is toggled.</param>
    /// <returns><see langword="true"/> if the reaction is present after the operation.</returns>
    bool ToggleReaction(string emoji, string user);
}
