namespace SecureMeet.Shared.Models;

// The meeting the user is in. IsHost = started it, otherwise joined it.
public sealed record MeetingInfo(string Id, string Port, bool IsHost);
