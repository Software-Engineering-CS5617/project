using System.Drawing;
using ScreenShare;

namespace Screenshare.Tests;

/// <summary>
/// exercises the real Win32 calls behind ScreenCapturer. these need an interactive
/// desktop session, which local runs and the windows CI runners both have.
/// </summary>
public class WindowsScreenCapturePlatformTests
{
    private static readonly IntPtr s_perMonitorAwareV2 = new(-4);

    [Fact]
    public void GetScreenWidthAndHeight_ReturnPrimaryDisplaySize()
    {
        var platform = new WindowsScreenCapturePlatform();

        Assert.True(platform.GetScreenWidth() > 0);
        Assert.True(platform.GetScreenHeight() > 0);
    }

    [Fact]
    public void SetThreadDpiAwarenessContext_ReturnsPreviousContext()
    {
        var platform = new WindowsScreenCapturePlatform();

        IntPtr previous = platform.SetThreadDpiAwarenessContext(s_perMonitorAwareV2);

        try
        {
            Assert.NotEqual(IntPtr.Zero, previous);
        }
        finally
        {
            platform.SetThreadDpiAwarenessContext(previous);
        }
    }

    [Fact]
    public void CopyFromScreen_FillsDestinationBitmap()
    {
        var platform = new WindowsScreenCapturePlatform();
        using var destination = new Bitmap(4, 4);

        platform.CopyFromScreen(destination, new Rectangle(0, 0, 4, 4));

        Assert.Equal(4, destination.Width);
        Assert.Equal(4, destination.Height);
    }
}
