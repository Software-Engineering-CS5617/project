namespace UI.ViewModels;

/// <summary>Participant row shown in the meeting people picker.</summary>
public sealed class ParticipantViewModel : AvatarViewModel
{
    public ParticipantViewModel(string username, string currentUsername, string hostUsername) : base(username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUsername);
        IsCurrentUser = string.Equals(username, currentUsername, StringComparison.Ordinal);
        IsHost = string.Equals(username, hostUsername, StringComparison.Ordinal);
    }

    public string Username => DisplayName;
    public bool IsCurrentUser { get; }
    public bool IsHost { get; }
    public string ParticipantRoleLabel => IsHost ? (IsCurrentUser ? "Host · You" : "Host") : IsCurrentUser ? "You" : string.Empty;
}
