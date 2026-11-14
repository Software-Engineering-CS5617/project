using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenShare;

/// <summary>
/// Captures the primary display on Windows.
/// </summary>
public class ScreenCapturer : IImageSource
{
    private static readonly IntPtr s_dpiAwarenessContextPerMonitorAwareV2 = new(-4);

    private readonly IScreenCapturePlatform _platform;

    /// <summary>
    /// Initializes the screen capturer using the Windows capture platform.
    /// </summary>
    public ScreenCapturer()
        : this(new WindowsScreenCapturePlatform())
    {
    }

    /// <summary>
    /// Initializes the screen capturer with a specified capture platform.
    /// </summary>
    /// <param name="platform">The platform used to capture the screen.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the platform is null.
    /// </exception>
    internal ScreenCapturer(IScreenCapturePlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);
        _platform = platform;
    }

    /// <summary>
    /// Captures the current contents of the primary display.
    /// </summary>
    /// <returns>A bitmap containing the captured screen.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the display dimensions are invalid.
    /// </exception>
    public Bitmap Capture()
    {
        IntPtr previousContext = _platform.SetThreadDpiAwarenessContext(s_dpiAwarenessContextPerMonitorAwareV2);

        try
        {
            return CaptureScreen();
        }
        finally
        {
            if (previousContext != IntPtr.Zero)
            {
                _platform.SetThreadDpiAwarenessContext(previousContext);
            }
        }
    }

    private Bitmap CaptureScreen()
    {
        int width = _platform.GetScreenWidth();
        int height = _platform.GetScreenHeight();

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException(
                $"Invalid screen dimensions: {width}x{height}");
        }

        var bounds = new Rectangle(0, 0, width, height);
        var screenshot = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        try
        {
            _platform.CopyFromScreen(screenshot, bounds);
            return screenshot;
        }
        catch
        {
            screenshot.Dispose();
            throw;
        }
    }
}
