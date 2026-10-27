namespace Controller;

// Centralizes the wire protocol for Controller messages.
// All message types, the separator character, and Format/Parse helpers
// Wire format: "TYPE:userId" or "TYPE:userId:ip:port"
// Notification format: "TYPE:payload"
public static class Protocol
{
    public const int ControllerSubscriberId = 1;
    public const string Join  = "JOIN";
    public const string Leave = "LEAVE";
    public const string Start = "START";
    public const string End   = "END";

    // notification format for diff types of notifications.
    public const string NotifyUserJoined   = "USER_JOINED";
    public const string NotifyUserLeft     = "USER_LEFT";
    public const string NotifyCountChanged = "COUNT_CHANGED";
    public const string NotifySessionEnded = "SESSION_ENDED";
    public const string NotifySessionStarted = "SESSION_STARTED";

    private const char Separator = ':';

    // Format helpers 
    // To join the meeting the format type is "join:userId:IP:port"
    public static string FormatJoinRequest(string userId, string senderIp, int senderPort)
        => $"{Join}{Separator}{userId}{Separator}{senderIp}{Separator}{senderPort}";

    // To leave a meeting the format type is "leave:userId"
    // where userId is taken at start. So, we don't need to specify userId once again.
    public static string FormatLeaveRequest(string userId)
        => $"{Leave}{Separator}{userId}";

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
    public string[] Extra { get; }

    public ParsedMessage(string type, string userId, string[] extra)
    {
        Type = type;
        UserId = userId;
        Extra = extra;
    }
}
