namespace ScreenShare;

/// <summary>
/// represents screen-sharing data that can be sent over the network
/// a packet contains a complete frame, changed screen tiles, or nothing (a pulse)
/// </summary>

public class ScreenDataPacket : IScreenShareMessage
{
    public ScreenShareMessageKind Kind => ScreenShareMessageKind.Data;

    /// <summary>
    /// gets the id of the screen-sharing stream the packet belongs to
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// gets the sequence number of the capture frame
    /// </summary>
    public long FrameNumber { get; }

    /// <summary>
    /// gets the time at which the frame was captured, in UTC
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// gets the width of the source frame in pixels
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// gets the height of the source frame in pixels
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// gets the size of a full tile in pixels. edge tiles may be smaller
    /// </summary>
    public int TileSize { get; }

    /// <summary>
    /// gets the type of screen data carried by this packet
    /// </summary>
    public DataPacketType Type { get; }

    /// <summary>
    /// gets whether this packet is a pulse sent for an unchanged screen
    /// </summary>
    public bool IsPulse => Type == DataPacketType.Pulse;

    /// <summary>
    /// gets whether this packet carries the complete frame
    /// </summary>
    public bool IsFullFrame => Type == DataPacketType.FullFrame;

    /// <summary>
    /// gets the encoded full-frame image, or null for tile updates and pulses
    /// </summary>
    public byte[]? FullFrameData { get; }

    /// <summary>
    /// gets the changed tiles, or an empty list for full-frame packets and pulses
    /// </summary>
    public IReadOnlyList<Tile> ChangedTiles { get; }

    /// <summary>
    /// creates a screen-data packet containing a full frame, changed tiles, or nothing (a pulse)
    /// </summary>
    /// <param name="streamId">id of the screen-sharing stream</param>
    /// <param name="frameNumber">sequence number of the frame</param>
    /// <param name="timestamp">capture time of the frame, in UTC</param>
    /// <param name="width">width of the source frame in pixels</param>
    /// <param name="height">height of the source frame in pixels</param>
    /// <param name="type">type of screen data</param>
    /// <param name="fullFrameData">encoded full-frame image, when applicable</param>
    /// <param name="changedTiles">changed tiles, when applicable</param>
    /// <param name="tileSize">size of a full tile in pixels</param>
    /// <exception cref="ArgumentException">
    /// thrown when the stream ID is empty, the frame number is negative, the timestamp is not UTC,
    /// or the frame or tile size is not positive
    /// </exception>
    /// <exception cref="ArgumentException">
    /// thrown when the payload does not match the packet type, or a tile does not fit the frame grid
    /// </exception>
    public ScreenDataPacket(
        string streamId,
        long frameNumber,
        DateTime timestamp,
        int width,
        int height,
        DataPacketType type,
        byte[]? fullFrameData = null,
        IReadOnlyList<Tile>? changedTiles = null,
        int tileSize = ImageDiffer.TILE_SIZE)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        if (frameNumber < 0)
        {
            throw new ArgumentException(
                "frame number cannot be negative",
                nameof(frameNumber));
        }

        // sender and receiver are different machines, so local time cannot be compared
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "timestamp must be in UTC",
                nameof(timestamp));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException(
                "frame dimensions must be positive",
                nameof(width));
        }

        if (tileSize <= 0)
        {
            throw new ArgumentException(
                "tile size must be positive",
                nameof(tileSize));
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
        else if (type == DataPacketType.ChangedTiles)
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

            foreach (Tile tile in changedTiles)
            {
                ValidateTile(tile, width, height, tileSize);
            }

            FullFrameData = null;
            ChangedTiles = Array.AsReadOnly(changedTiles.ToArray());
        }
        else
        {
            if (fullFrameData is { Length: > 0 } || changedTiles is { Count: > 0 })
            {
                throw new ArgumentException(
                    "a pulse packet cannot contain image data or tiles",
                    fullFrameData is { Length: > 0 } ? nameof(fullFrameData) : nameof(changedTiles));
            }

            FullFrameData = null;
            ChangedTiles = Array.Empty<Tile>();
        }

        StreamId = streamId;
        FrameNumber = frameNumber;
        Timestamp = timestamp;
        Width = width;
        Height = height;
        TileSize = tileSize;
        Type = type;
    }

    /// <summary>
    /// gets the column and row of a tile in this packet's tile grid
    /// </summary>
    public (int TileX, int TileY) GetTileIndex(Tile tile)
    {
        ArgumentNullException.ThrowIfNull(tile);

        return (tile.X / TileSize, tile.Y / TileSize);
    }

    /// <summary>
    /// checks that a tile starts on the tile grid, is no larger than a tile, and lies inside the frame
    /// </summary>
    private static void ValidateTile(Tile tile, int width, int height, int tileSize)
    {
        if (tile == null)
        {
            throw new ArgumentException(
                "changed tiles cannot contain null",
                nameof(tile));
        }

        if (tile.X % tileSize != 0 || tile.Y % tileSize != 0)
        {
            throw new ArgumentException(
                "tile must start on the tile grid",
                nameof(tile));
        }

        if (tile.Width > tileSize || tile.Height > tileSize)
        {
            throw new ArgumentException(
                "tile cannot be larger than the tile size",
                nameof(tile));
        }

        // using subtraction to avoid integer overflow in X+width or Y+height
        if (tile.X > width - tile.Width || tile.Y > height - tile.Height)
        {
            throw new ArgumentException(
                "tile lies outside the frame",
                nameof(tile));
        }
    }
}
