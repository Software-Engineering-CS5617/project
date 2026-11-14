namespace ScreenShare;

/// <summary>
/// message that IScreenShareCodec can put on the network: screen data or control msg etc
/// </summary>
public interface IScreenShareMessage
{
    /// <summary>
    /// gets the kind written in the message header
    /// </summary>
    ScreenShareMessageKind Kind { get; }
}

