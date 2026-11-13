namespace Chat;

/// <summary>
/// Represents the users mentioned in a message.
/// </summary>
public sealed class Mentions : IMentions
{
    /// <summary>
    /// Indicates whether every participant has been mentioned.
    /// </summary>
    public bool All { get; init; }

    /// <summary>
    /// Stores explicit usernames mentioned by a message.
    /// </summary>
    public HashSet<string> Usernames { get; init; } = new();

    /// <summary>
    /// Creates a copy of these mentions.
    /// </summary>
    /// <returns>A new mentions object with the same values.</returns>
    public Mentions Clone()
    {
        return new Mentions
        {
            All = All,
            Usernames = new HashSet<string>(Usernames, Usernames.Comparer)
        };
    }

    /// <inheritdoc />
    public bool IsUserMentioned(string username)
    {
        ArgumentNullException.ThrowIfNull(username);
        return All || Usernames.Contains(username);
    }
}
