using System.Text.Json.Serialization;

namespace SecureMeet.Shared.Models;

// A user, as every module sees them. Read the logged-in user from session.CurrentUser.
public sealed class UserProfile
{
    public string UserId { get; init; } = "";
    public string Email { get; init; } = "";
    public bool HasProfilePicture { get; init; }   // only a flag for now

    [JsonIgnore]
    public string Username => UserId;
}
