namespace ScreenShare;


/// <summary>
/// a person in the meeting as reported by the meeting controller
/// </summary>
public sealed record MeetingParticipant
{
    public string Id { get; }
    public string DisplayName { get; }
    public bool IsHost { get; }

    /// <summary>
    /// <excpetion cref="ArgumentException"> id is Null or white space </excpetion>
    /// <exception cref="ArgumentException"> displayname is Null or whiteSpace</exception>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="displayName"></param>
    /// <param name="isHost"></param>
    public MeetingParticipant(string id, string displayName, bool isHost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        Id = id;
        DisplayName = displayName;
        IsHost = isHost;
    }

}
