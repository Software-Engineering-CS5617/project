namespace ScreenShare;

/// <summary>
/// represents a changed rectangular region of a screen frame
/// </summary>
public sealed class Tile
{
    /// <summary>Gets the tile horizontal position</summary>
    public int X { get; }

    /// <summary>gets the tile vertical position</summary>
    public int Y { get; }

    /// <summary>gets the tile width in pixels</summary>
    public int Width { get; }

    /// <summary>gets the tile height in pixels</summary>
    public int Height { get; }

    /// <summary>gets the hash of the tile pixel data</summary>
    public ulong Hash { get; }

    private readonly byte[] _data;

    /// <summary>
    /// Gets a copy of the encoded tile image data
    /// </summary>
    public byte[] Data => (byte[])_data.Clone();

    /// <summary>
    /// creates a tile containing its position, dimensions, hash and image data
    /// </summary>
    public Tile(int x, int y, int width, int height, ulong hash, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (x < 0 || y < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x), "Tile coordinates cannot be negative.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width), "Tile dimensions must be positive.");
        }

        if (data.Length == 0)
        {
            throw new ArgumentException(
                "Tile image data cannot be empty.", nameof(data));
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
        Hash = hash;
        _data = (byte[])data.Clone();
    }
}
