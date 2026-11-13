using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScreenShare;

public class ImageStitcher : IImageStitcher, IDisposable
{
    private Bitmap? _currentFrame;

    public long LastFrameNumber { get; private set; } = -1;

    public bool NeedsFullFrame { get; private set; } = true;

    /// <summary>
    /// replaces the current frame when the given frame is newer than the last accepted packet.
    /// </summary>
    /// <exception cref="ArgumentNullException">The frame is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The frame number is negative.</exception>
    public bool ApplyFullFrame(long frameNumber, Bitmap frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentOutOfRangeException.ThrowIfNegative(frameNumber);

        if (frameNumber <= LastFrameNumber)
        {
            return false;
        }

        Bitmap newFrame = new Bitmap(frame);
        _currentFrame?.Dispose();
        _currentFrame = newFrame;

        LastFrameNumber = frameNumber;
        NeedsFullFrame = false;
        return true;
    }

    /// <summary>
    /// Applies a changed tile to the current frame after validating its bounds. either all tirles drawn or none
    /// </summary>
    /// <param name="frameNumber">The frame the tiles belong to.</param>
    /// <param name="tiles">The changed tiles received from the sender.</param>
    /// <exception cref="ArgumentNullException">The tile list or a tile is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The frame number is negative.</exception>
    /// <exception cref="ArgumentException">The list is empty, or a tile has invalid dimensions or coordinates.</exception>
    public bool ApplyChangedTiles(long frameNumber, IReadOnlyList<Tile> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentOutOfRangeException.ThrowIfNegative(frameNumber);

        if (tiles.Count == 0)
        {
            throw new ArgumentException("a delta must contain at least one tile", nameof(tiles));
        }

        if (!TryAdvance(frameNumber))
        {
            return false;
        }

        // decode and validate every tile first, so a bad tile cannot leave the frame half updated
        List<(Tile Tile, Bitmap Bitmap)> decoded = new(tiles.Count);
        try
        {
            foreach (Tile tile in tiles)
            {
                decoded.Add((tile, DecodeTile(tile)));
            }

            using Graphics graphics = Graphics.FromImage(_currentFrame!);

            // tiles replace pixels; the default SourceOver would blend non-opaque pixels with the old frame
            graphics.CompositingMode = CompositingMode.SourceCopy;

            foreach ((Tile tile, Bitmap tileBitmap) in decoded)
            {
                graphics.DrawImage(
                    tileBitmap,
                    new Rectangle(tile.X, tile.Y, tile.Width, tile.Height),
                    0,
                    0,
                    tileBitmap.Width,
                    tileBitmap.Height,
                    GraphicsUnit.Pixel
                    );
            }
        }
        finally
        {
            foreach ((_, Bitmap tileBitmap) in decoded)
            {
                tileBitmap.Dispose();
            }
        }

        LastFrameNumber = frameNumber;
        return true;
    }

    /// <summary>
    /// records an unchanged frame; the current frame is kept as it is.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The frame number is negative.</exception>
    public bool ApplyPulse(long frameNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frameNumber);

        if (!TryAdvance(frameNumber))
        {
            return false;
        }

        LastFrameNumber = frameNumber;
        return true;
    }



    public Bitmap? GetCurrentFrame()
    {
        return _currentFrame == null ? null : new Bitmap(_currentFrame);
    }

    public void Reset()
    {
        _currentFrame?.Dispose();
        _currentFrame = null;
        LastFrameNumber = -1;
        NeedsFullFrame = true;
    }

    public void Dispose()
    {
        Reset();
    }

    /// <summary>
    /// check whether a delta or pulse with this frame number can be applied.
    /// a gap means a frame was missed, so every later delta would be relative to a frame we did not saw
    /// </summary>
    private bool TryAdvance(long frameNumber)
    {
        if (NeedsFullFrame || frameNumber <= LastFrameNumber)
        {
            return false;
        }

        if (frameNumber != LastFrameNumber + 1)
        {
            NeedsFullFrame = true;
            return false;
        }

        return true;
    }

    private Bitmap DecodeTile(Tile tile)
    {
        ArgumentNullException.ThrowIfNull(tile);

        // using subtraction to avoid integer overflow in X+width or Y+height
        if (tile.X > _currentFrame!.Width - tile.Width ||
            tile.Y > _currentFrame.Height - tile.Height)
        {
            throw new ArgumentException("tile lies outside the current frame", nameof(tile));
        }

        // Tile.Data returns a fresh copy on every read, so read it once
        byte[] data = tile.Data;

        // ImageDiffer stores the changed tile as PNG bytes. decode the bytes back to a bitmap
        // Bitmap keeps reading from its stream, so copy it into a standalone bitmap.
        Bitmap tileBitmap;
        using (MemoryStream stream = new(data))
        using (Bitmap streamBitmap = new(stream))
        {
            tileBitmap = new Bitmap(streamBitmap);
        }

        //  the encoded image dimensions should mathch the tile metadata
        if (tileBitmap.Width != tile.Width || tileBitmap.Height != tile.Height)
        {
            tileBitmap.Dispose();
            throw new ArgumentException(
                "encoded tile dimensions do not match the tile metadata", nameof(tile));
        }

        return tileBitmap;
    }

}
