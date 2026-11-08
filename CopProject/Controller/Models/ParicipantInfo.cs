using Controller.ViewModels;

namespace Controller.Models;

public sealed class ParticipantInfo : ObservableObject
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public byte[]? ProfilePicture { get; set; }

    public string RoleDisplay => IsHost ? "Host" : "Client";
    public string DisplayName => IsHost ? $"{Username} (Host)" : Username;
}