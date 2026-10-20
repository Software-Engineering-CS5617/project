namespace ScreenShare;

/// <summary>
/// specifies the type of screen-sharing data carried by a packet.
/// </summary>
public enum DataPacketType
{
    /// <summary>
    /// indicates that the packet contains a complete screen frame.
    /// </summary>
    FullFrame,

    /// <summary>
    /// indicates that the packet contains only changed screen tiles.
    /// </summary>
    ChangedTiles
}
