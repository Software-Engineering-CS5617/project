using System.Drawing;
using System.Drawing.Imaging;
using ScreenShare;

namespace Screenshare.Tests;

public class ScreenCapturerTests
{
    [Fact]
    public void Capture_ReturnsBitmapWithCorrectDimensions()
    {
        var platform = new FakeScreenCapturePlatform {
            Width = 1920,
            Height = 1080
        };

        var capturer = new ScreenCapturer(platform);

        using Bitmap result = capturer.Capture();

        Assert.Equal(1920, result.Width);
        Assert.Equal(1080, result.Height);
        Assert.Equal(PixelFormat.Format32bppArgb, result.PixelFormat);
        Assert.Equal(Color.CornflowerBlue.ToArgb(),
            result.GetPixel(10, 10).ToArgb());

        Assert.Equal(1, platform.CopyCallCount);
        Assert.Equal(new Rectangle(0, 0, 1920, 1080), platform.LastBounds);
    }

    [Fact]
    public void Capture_RestoresPreviousDpiContext()
    {
        IntPtr previousContext = new IntPtr(123);

        var platform = new FakeScreenCapturePlatform {
            Width = 800,
            Height = 600,
            PreviousDpiContext = previousContext
        };

        var capturer = new ScreenCapturer(platform);

        using Bitmap result = capturer.Capture();

        Assert.Equal(2, platform.DpiContextsSet.Count);

        // first call sets PerMonitorV2 awareness
        Assert.Equal(new IntPtr(-4), platform.DpiContextsSet[0]);

        // fecond call restores the previous context
        Assert.Equal(previousContext, platform.DpiContextsSet[1]);
    }

    [Fact]
    public void Capture_DoesNotRestoreDpiContextWhenPreviousContextIsZero()
    {
        var platform = new FakeScreenCapturePlatform {
            Width = 800,
            Height = 600,
            PreviousDpiContext = IntPtr.Zero
        };

        var capturer = new ScreenCapturer(platform);

        using Bitmap result = capturer.Capture();

        Assert.Single(platform.DpiContextsSet);
        Assert.Equal(new IntPtr(-4), platform.DpiContextsSet[0]);
    }

    [Theory]
    [InlineData(0, 600)]
    [InlineData(-1, 600)]
    [InlineData(800, 0)]
    [InlineData(800, -1)]
    public void Capture_ThrowsWhenScreenDimensionsAreInvalid(int width, int height)
    {
        IntPtr previousContext = new IntPtr(123);

        var platform = new FakeScreenCapturePlatform {
            Width = width,
            Height = height,
            PreviousDpiContext = previousContext
        };

        var capturer = new ScreenCapturer(platform);

        Assert.Throws<InvalidOperationException>(() => { capturer.Capture(); });

        // DPI context should still be restored after the exception
        Assert.Equal(2, platform.DpiContextsSet.Count);
        Assert.Equal(previousContext, platform.DpiContextsSet[1]);

        // capture should not be attempted with invalid dimensions
        Assert.Equal(0, platform.CopyCallCount);
    }

    [Fact]
    public void Capture_RestoresDpiContextWhenScreenCaptureFails()
    {
        IntPtr previousContext = new IntPtr(123);
        var expectedException = new InvalidOperationException("Capture failed.");

        var platform = new FakeScreenCapturePlatform {
            Width = 800,
            Height = 600,
            PreviousDpiContext = previousContext,
            CopyException = expectedException
        };

        var capturer = new ScreenCapturer(platform);

        InvalidOperationException actualException = Assert.Throws<InvalidOperationException>(
            () => { capturer.Capture(); });

        Assert.Same(expectedException, actualException);

        //  DPI context must be restored even when copying fails
        Assert.Equal(2, platform.DpiContextsSet.Count);
        Assert.Equal(previousContext, platform.DpiContextsSet[1]);
        Assert.Equal(1, platform.CopyCallCount);
    }

    [Fact]
    public void Constructor_ThrowsWhenPlatformIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ScreenCapturer(null!));
    }

    [Fact]
    public void DefaultConstructor_UsesWindowsPlatform()
    {
        var capturer = new ScreenCapturer();

        Assert.IsAssignableFrom<IImageSource>(capturer);
    }
    private sealed class FakeScreenCapturePlatform : IScreenCapturePlatform
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public IntPtr PreviousDpiContext { get; set; }

        public Exception? CopyException { get; set; }

        public int CopyCallCount { get; private set; }

        public Rectangle LastBounds { get; private set; }

        public List<IntPtr> DpiContextsSet { get; } = new();

        public int GetScreenWidth()
        {
            return Width;
        }

        public int GetScreenHeight()
        {
            return Height;
        }

        public IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext)
        {
            DpiContextsSet.Add(dpiContext);

            return DpiContextsSet.Count == 1
                ? PreviousDpiContext
                : IntPtr.Zero;
        }

        public void CopyFromScreen(Bitmap destination, Rectangle bounds)
        {
            CopyCallCount++;
            LastBounds = bounds;

            if (CopyException != null)
            {
                throw CopyException;
            }

            using Graphics graphics = Graphics.FromImage(destination);
            graphics.Clear(Color.CornflowerBlue);
        }
    }
}
