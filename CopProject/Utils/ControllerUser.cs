namespace Utils;

// Stores user identity and networking information for a participant.
public class ControllerUser
{
    public string Username { get; set; } = string.Empty;
    public string Gmail { get; set; } = string.Empty;
    // Raw byte array representing the user's profile image.
    public byte[]? ProfilePicture { get; set; }

    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool IsHost { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Parameterless constructor 
    public ControllerUser() { }

    // Parameterized constructor
    public ControllerUser(string username, string gmail, byte[]? profilePicture, string ip, int port, bool isHost = false)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Gmail = gmail ?? throw new ArgumentNullException(nameof(gmail));
        ProfilePicture = profilePicture;
        Ip = ip ?? throw new ArgumentNullException(nameof(ip));
        Port = port;
        IsHost=isHost;
    }
    public string UserKey => $"{Gmail}#{Ip}#{Port}";
    public override string ToString() => $"[{(IsHost ? "Host" : "Participant")}]{Username} ({Gmail}) @ {Ip}:{Port}";
    public string RoleDisplay => IsHost ? "Host" : "Client";
}
