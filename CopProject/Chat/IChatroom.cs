namespace Chat;

/// <summary>
/// Describes operations for a chatroom.
/// </summary>
public interface IChatroom
{
    /// <summary>
    /// Adds a participant to the room.
    /// </summary>
    /// <param name="name">The participant's username.</param>
    /// <returns><see langword="true"/> if the participant was added.</returns>
    bool AddParticipant(string name);

    /// <summary>
    /// Removes a participant from the room.
    /// </summary>
    /// <param name="name">The participant's username.</param>
    /// <returns><see langword="true"/> if the participant was removed.</returns>
    bool RemoveParticipant(string name);

    /// <summary>
    /// Adds a message to the room.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <returns>The ID assigned to the message.</returns>
    Guid AddMessage(ChatMessageWithThreads message);

    /// <summary>
    /// Adds a reply to a message's thread.
    /// </summary>
    /// <param name="parentMessageId">The ID of the thread's root message.</param>
    /// <param name="reply">The reply to add.</param>
    /// <returns>The ID assigned to the reply.</returns>
    Guid AddThreadMessage(Guid parentMessageId, ChatMessage reply);

    /// <summary>
    /// Finds a message by ID.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="message">Receives the message if it exists.</param>
    /// <returns><see langword="true"/> if the message was found.</returns>
    bool TryGetMessage(Guid id, out ChatMessage message);

    /// <summary>
    /// Edits a message when the editor is its sender.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="newContent">The replacement content.</param>
    /// <param name="editor">The username attempting the edit.</param>
    /// <param name="parentMessageId">The thread root ID, or <see langword="null"/> for a room message.</param>
    /// <returns>The result of the operation.</returns>
    RoomResult EditMessage(Guid id, string newContent, string editor, Guid? parentMessageId = null);

    /// <summary>
    /// Deletes a message when the deleter is its sender.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="deleter">The username attempting the deletion.</param>
    /// <param name="parentMessageId">The thread root ID, or <see langword="null"/> for a room message.</param>
    /// <returns>The result of the operation.</returns>
    RoomResult DeleteMessage(Guid id, string deleter, Guid? parentMessageId = null);

    /// <summary>
    /// Toggles a user's reaction on a message.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="emoji">The reaction emoji.</param>
    /// <param name="user">The username whose reaction is toggled.</param>
    /// <param name="parentMessageId">The thread root ID, or <see langword="null"/> for a room message.</param>
    /// <returns>The result of the operation.</returns>
    RoomResult ToggleReaction(Guid id, string emoji, string user, Guid? parentMessageId = null);
}

/// <summary>
/// Describes the result of a chatroom operation.
/// </summary>
public enum RoomResult
{
    /// <summary>The operation completed successfully.</summary>
    Ok,

    /// <summary>The requested message or thread does not exist.</summary>
    NotFound,

    /// <summary>The user is not permitted to perform the operation.</summary>
    Forbidden
}
