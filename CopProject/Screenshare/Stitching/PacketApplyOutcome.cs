namespace ScreenShare;

/// <summary>
/// what happened when a received data packet was applied to a stream
/// </summary>
public enum PacketApplyOutcome
{
    /// <summary>the packet was applied, or the pulse was accepted</summary>
    Applied,

    /// <summary>the packet was stale, a duplicate or corrupt, and was dropped</summary>
    Ignored,

    /// <summary>the packet was dropped because the stream is waiting for a full frame</summary>
    NeedsFullFrame
}
