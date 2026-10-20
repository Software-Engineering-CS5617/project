namespace ScreenShare;

/// <summary>
/// represents screen-sharing data that can be sent over the network
/// a packet contains either a complete frame or changed screen tiles
/// </summary>

public class ScreenDataPacket
{
    /// <summary>
    /// gets the identifer of the screen-sharing stream this packet belongs to
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// gets the sequence number of the capture frame
    /// </summary>
    public long FrameNumber { get; }

    /// <summary>
    /// gets the type of screen data carried by this packet
    /// </summary>
    public DataPacketType Type { get; }

    /// <summary>
    /// gets the encoded full-frame image, or null when this is a tile update
    /// </summary>
    public byte[]? FullFrameData { get; }

    /// <summary>
    /// gets the changed tiles, or an empty list for a full-frame packet
    /// </summary>
    public IReadOnlyList<Tile> ChangedTiles { get; }

    /// <summary>
    /// creates a screen-data packet containing either a full frame or changed tiles
    /// </summary>
    /// <param name="streamId">identifier of the screen-sharing stream</param>
    /// <param name="frameNumber">sequence number of the frame</param>
    /// <param name="type">type of screen data</param>
    /// <param name="fullFrameData">encoded full-frame image, when applicable</param>
    /// <param name="changedTiles">changed tiles, when applicable</param>
    /// <exception cref="ArgumentException">
    /// thrown when the stream ID is empty or the frame number is negative
    /// </exception>
    /// <exception cref="ArgumentException">
    /// thrown when the payload does not match the packet type
    /// </exception>
    public ScreenDataPacket(
        string streamId,
        long frameNumber,
        DataPacketType type,
        byte[]? fullFrameData = null,
        IReadOnlyList<Tile>? changedTiles = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        if (frameNumber < 0)
        {
            throw new ArgumentException(
                "frame number cannot be negative",
                nameof(frameNumber));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException(
                "invalid data packet type",
                nameof(type));
        }

        if (type == DataPacketType.FullFrame)
        {
            if (fullFrameData == null || fullFrameData.Length == 0)
            {
                throw new ArgumentException(
                    "a fullframe packet must contain image data",
                    nameof(fullFrameData));
            }

            if (changedTiles is { Count: > 0 })
            {
                throw new ArgumentException(
                    "a full frame packet cannot also contain changed tiles",
                    nameof(changedTiles));
            }

            FullFrameData = (byte[])fullFrameData.Clone();
            ChangedTiles = Array.Empty<Tile>();
        }
        else
        {
            if (changedTiles == null || changedTiles.Count == 0)
            {
                throw new ArgumentException(
                    "a changed-tile packet must contain at least one tile",
                    nameof(changedTiles));
            }

            if (fullFrameData is { Length: > 0 })
            {
                throw new ArgumentException(
                    "a changed-tile packet cannot also contain a full frame",
                    nameof(fullFrameData));
            }

            FullFrameData = null;
            ChangedTiles = Array.AsReadOnly(changedTiles.ToArray());
        }

        StreamId = streamId;
        FrameNumber = frameNumber;
        Type = type;
    }
}
