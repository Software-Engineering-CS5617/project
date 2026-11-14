using System.Drawing;
using System.Diagnostics;

namespace ScreenShare;

/// <summary>
/// Coordinates screen capture and frame comparison
/// </summary>
public class ScreenSharingService : IDisposable
{
    /// <summary>
    /// how many captures in a row a small change may be held back before it is sent anyway
    /// (12 frames is half a second at 24 fps)
    /// </summary>
    public const int MAX_HELD_FRAMES = 12;
    private readonly IImageSource _imageSource;

    private readonly IImageDiffer _imageDiffer;

    // the last frame actually sent. while small changes are held back it stays put,
    // so those changes pile up and go out together in a later delta
    private Bitmap? _previousFrame;
    private int _heldFrames;
    private bool _disposed;

    /// <summary>
    /// Creates the screen sharing service
    /// </summary>
    public ScreenSharingService(IImageSource imageSource, IImageDiffer imageDiffer)
    {

        ArgumentNullException.ThrowIfNull(imageSource);
        ArgumentNullException.ThrowIfNull(imageDiffer);

        _imageSource = imageSource;
        _imageDiffer = imageDiffer;
    }

    /// <summary>
    /// Captures the current frame and compares it with the previous frame.
    /// The first frame is always returned as a full frame.
    /// The caller owns the returned result and must dispose it.
    /// </summary>
    public ImageDiffResult ProcessFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using Bitmap currentFrame = _imageSource.Capture();

        // first frame has nothing to compare against
        // so, the first frame must be treated as a full frame.
        if (_previousFrame == null)
        {
            Trace.TraceInformation(
            "[Screenshare] First frame captured. Dimensions: {0}x{1}. Sending full frame.",
            currentFrame.Width,
            currentFrame.Height);

            _previousFrame = new Bitmap(currentFrame);

            return ImageDiffResult.CreateFullFrame(new Bitmap(_previousFrame));
        }

        ImageDiffResult result = _imageDiffer.Compare(_previousFrame, currentFrame);

        // a tiny change is reported as unchanged (sent as a pulse) and the base frame is kept,
        // so the change is not lost: it is sent once enough piles up or it has waited too long
        if (IsSmallChange(result, currentFrame) && _heldFrames < MAX_HELD_FRAMES)
        {
            _heldFrames++;
            result.Dispose();
            return ImageDiffResult.Unchanged();
        }

        _heldFrames = 0;

        // Current frame becomes the base frame for the next comparison.
        Bitmap newPreviousFrame = new Bitmap(currentFrame);

        _previousFrame.Dispose();

        _previousFrame = newPreviousFrame;

        return result;
    }

    /// <summary>
    /// true for a delta whose changed tiles cover at most MIN_CHANGE_THRESHOLD of the frame
    /// </summary>
    private static bool IsSmallChange(ImageDiffResult result, Bitmap frame)
    {
        if (result.IsUnchanged || result.IsFullFrame)
        {
            return false;
        }

        long changedPixels = 0;
        foreach (Tile tile in result.ChangedTiles)
        {
            changedPixels += (long)tile.Width * tile.Height;
        }

        long totalPixels = (long)frame.Width * frame.Height;
        return (double)changedPixels / totalPixels <= ImageDiffer.MIN_CHANGE_THRESHOLD;
    }

    /// <summary>
    /// Releases the stored frame and resets the comparison state
    /// </summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _previousFrame?.Dispose();

        _previousFrame = null;
        _heldFrames = 0;

        Trace.TraceInformation("[Screenshare] Screen sharing service reset. Previous frame cleared.");
    }

    /// <summary>
    /// Releases all resources owned by this service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _previousFrame?.Dispose();
        _previousFrame = null;

        _disposed = true;
        Trace.TraceInformation("[Screenshare] Screen sharing service disposed.");
    }
}
