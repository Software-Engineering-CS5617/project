using System.Drawing;

namespace ScreenShare;

/// <summary>
/// Coordinates screen capture and frame comparison
/// </summary>
public class ScreenSharingService : IDisposable
{
    private readonly IImageSource _imageSource;

    private readonly IImageDiffer _imageDiffer;

    private Bitmap? _previousFrame;
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

        // first frame has nothing to compare against.
        // so, the first frame must be treated as a full frame.
        if (_previousFrame == null)
        {
            _previousFrame = new Bitmap(currentFrame);

            return ImageDiffResult.CreateFullFrame(new Bitmap(_previousFrame));
        }

        ImageDiffResult result = _imageDiffer.Compare(_previousFrame, currentFrame);

        // Current frame becomes the base frame for the next comparison.
        Bitmap newPreviousFrame = new Bitmap(currentFrame);

        _previousFrame.Dispose();

        _previousFrame = newPreviousFrame;

        return result;
    }

    /// <summary>
    /// Releases the stored frame and resets the comparison state
    /// </summary>

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _previousFrame?.Dispose();

        _previousFrame = null;
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
    }
}
