using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScreenShare;

/// <summary>
/// scales captures down to a max height, keeping the aspect ratio (like 1920x1080 becomes 1280x720). smaller captures pass through unchanged.
/// </summary>
public sealed class ScalingImageSource : IImageSource, IDisposable
{
    private readonly IImageSource _inner;

    /// <summary>get the hieght limit</summary>
    public int MaxHeight { get; }

    /// <param name="inner">the source to scale; disposed with this one if it is disposable</param>
    /// <param name="maxHeight">the largest height to return</param>
    /// <exception cref="ArgumentNullException">the inner source is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">the height is not positive</exception>
    public ScalingImageSource(IImageSource inner, int maxHeight)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHeight);

        _inner = inner;
        MaxHeight = maxHeight;
    }

    public Bitmap Capture()
    {
        Bitmap frame = _inner.Capture();

        if (frame.Height <= MaxHeight)
        {
            return frame;
        }

        using (frame)
        {
            int width = Math.Max(1, (int)Math.Round((double)frame.Width * MaxHeight / frame.Height));
            var scaled = new Bitmap(width, MaxHeight, PixelFormat.Format32bppArgb);

            try
            {
                using Graphics graphics = Graphics.FromImage(scaled);
                using var attributes = new ImageAttributes();

                // without this the filter samples outside the image and darkens the edges
                attributes.SetWrapMode(WrapMode.TileFlipXY);

                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(
                    frame,
                    new Rectangle(0, 0, width, MaxHeight),
                    0,
                    0,
                    frame.Width,
                    frame.Height,
                    GraphicsUnit.Pixel,
                    attributes);

                return scaled;
            }
            catch
            {
                scaled.Dispose();
                throw;
            }
        }
    }

    public void Dispose()
    {
        (_inner as IDisposable)?.Dispose();
    }
}
