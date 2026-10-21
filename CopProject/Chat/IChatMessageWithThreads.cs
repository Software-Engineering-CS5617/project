namespace Chat;

/// <summary>
/// Describes a chat message that can contain one level of threaded replies.
/// </summary>
public interface IChatMessageWithThreads : IChatMessage
{
    /// <summary>
    /// Adds a reply to this message's thread.
    /// </summary>
    /// <param name="reply">The reply to add.</param>
    void AddMessageToThread(ChatMessage reply);
}
