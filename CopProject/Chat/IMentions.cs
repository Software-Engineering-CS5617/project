namespace Chat;

/// <summary>
/// Operations related to mentions in a chat message.
/// </summary>
public interface IMentions
{
    /// <summary>
    /// Determines whether a user is mentioned by a Mention object.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <returns><see langword="true"/> when the user is mentioned.</returns>
    bool IsUserMentioned(string username);
}