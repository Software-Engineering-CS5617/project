using System;

namespace Cloud.UI.Models;

/// <summary>
/// One entry in the notification list (UI model; built from a Cloud.Networking.Message).
/// </summary>
public sealed record NotificationItem
{
    /// <summary>message id.</summary>
    public required Guid Id { get; init; }

    /// <summary>message sender id</summary>
    public required string SenderId { get; init; }

    /// <summary>message content</summary>
    public required string Content { get; init; }

    /// <summary>Tiem at which message reached client</summary>
    public required DateTimeOffset ReceivedAt { get; init; }

    /// <summary>Local time shown</summary>
    public string TimeText => ReceivedAt.ToLocalTime().ToString("t");

    /// <summary>Second line shown under the content.</summary>
    public string Subtitle => SenderId + " · " + TimeText;
}
