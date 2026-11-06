using System.Text.Json;
using Utils;
namespace Controller;

// Centralizes the wire protocol for Controller messages.

public static class Protocol
{
    public const int ControllerSubscriberId = 1;
    public const string Join  = "JOIN";
    public const string Leave = "LEAVE";
    public const string End   = "END";

    // notification format for diff types of notifications.
    public const string NotifyUserJoined   = "USER_JOINED";
    public const string NotifyUserLeft     = "USER_LEFT";
    public const string NotifySessionEnded = "SESSION_ENDED";

    public const char Separator = '|';

    // Format helpers 
    public static string FormatJoinRequest(ControllerUser user)
        => $"{Join}{Separator}{JsonSerializer.Serialize(user)}";

    public static string FormatLeaveRequest(string userKey)
        => $"{Leave}{Separator}{userKey}";

    // A notification sent from host back to participants.
    public static string FormatNotification(string notificationType, string payload)
        => $"{notificationType}{Separator}{payload}";

    // Parses a raw message string into its parts.
    // Returns null if the message is malformed.
    public static ParsedMessage? Parse(string rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
            return null;

        string[] parts = rawMessage.Split(Separator);
        if (parts.Length < 2)
            return null;

        return new ParsedMessage(
            type: parts[0],
            userId: parts[1],
            extra: parts.Length > 2 ? parts[2..] : Array.Empty<string>()
        );
    }
}

// Represents a parsed Controller protocol message.
public sealed class ParsedMessage
{
    public string Type { get; }
    public string UserId { get; }
    public string Payload => UserId;
    public string[] Extra { get; }

    public ParsedMessage(string type, string userId, string[] extra)
    {
        Type = type;
        UserId = userId;
        Extra = extra;
    }
}
