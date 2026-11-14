using System.Drawing;
using ScreenShare;

namespace Screenshare.Tests;

public class ScalingImageSourceTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ScalingImageSource(null!, 720));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidMaxHeight_ThrowsArgumentOutOfRangeException(
        int maxHeight)
    {
        using var source = new FakeImageSource(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScalingImageSource(source, maxHeight));
    }

    [Fact]
    public void Constructor_ValidArguments_SetsMaxHeight()
    {
        using var source = new FakeImageSource(100, 100);

        using var scalingSource = new ScalingImageSource(source, 720);

        Assert.Equal(720, scalingSource.MaxHeight);
    }

    [Fact]
    public void Capture_FrameAlreadyWithinLimit_ReturnsOriginalFrame()
    {
        using var source = new FakeImageSource(800, 600);
        using var scalingSource = new ScalingImageSource(source, 720);

        Bitmap result = scalingSource.Capture();

        Assert.Same(source.LastCapturedBitmap, result);
        Assert.Equal(800, result.Width);
        Assert.Equal(600, result.Height);

        result.Dispose();
    }

    [Fact]
    public void Capture_TallFrame_ScalesAndPreservesAspectRatio()
    {
        using var source = new FakeImageSource(1920, 1080);
        using var scalingSource = new ScalingImageSource(source, 720);

        using Bitmap result = scalingSource.Capture();

        Assert.Equal(1280, result.Width);
        Assert.Equal(720, result.Height);
    }

    [Fact]
    public void Capture_VeryNarrowFrame_ProducesAtLeastOnePixelWidth()
    {
        using var source = new FakeImageSource(1, 1000);
        using var scalingSource = new ScalingImageSource(source, 1);

        using Bitmap result = scalingSource.Capture();

        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
    }

    [Fact]
    public void Dispose_DisposableInner_DisposesInnerSource()
    {
        var source = new FakeImageSource(100, 100);
        var scalingSource = new ScalingImageSource(source, 720);

        scalingSource.Dispose();

        Assert.True(source.IsDisposed);
    }

    [Fact]
    public void Dispose_NonDisposableInner_DoesNotThrow()
    {
        var source = new NonDisposableImageSource();
        var scalingSource = new ScalingImageSource(source, 720);

        scalingSource.Dispose();
    }

    private sealed class FakeImageSource : IImageSource, IDisposable
    {
        private readonly int _width;
        private readonly int _height;

        public Bitmap? LastCapturedBitmap { get; private set; }
        public bool IsDisposed { get; private set; }

        public FakeImageSource(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public Bitmap Capture()
        {
            LastCapturedBitmap = new Bitmap(_width, _height);
            return LastCapturedBitmap;
        }

        public void Dispose()
        {
            IsDisposed = true;
            LastCapturedBitmap?.Dispose();
        }
    }

    [Fact]
    public void Capture_ScalingFails_PropagatesExceptionAndDisposesFrame()
    {
        // GDI+ cannot draw 16bpp grayscale bitmaps, so DrawImage throws while scaling
        var source = new UndrawableImageSource();
        using var scalingSource = new ScalingImageSource(source, 720);

        Assert.ThrowsAny<Exception>(() => { scalingSource.Capture(); });

        // the captured frame was disposed by the scaler, so reading it fails
        Assert.NotNull(source.LastCapturedBitmap);
        Assert.ThrowsAny<ArgumentException>(() => source.LastCapturedBitmap!.Width);
    }

    private sealed class UndrawableImageSource : IImageSource
    {
        public Bitmap? LastCapturedBitmap { get; private set; }

        public Bitmap Capture()
        {
            LastCapturedBitmap = new Bitmap(100, 1000, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
            return LastCapturedBitmap;
        }
    }

    private sealed class NonDisposableImageSource : IImageSource
    {
        public Bitmap Capture()
        {
            return new Bitmap(100, 100);
        }
    }

    [Fact]
    public void Capture_FrameHeightExactlyAtLimit_ReturnsOriginalFrame()
    {
        FakeImageSource source = new FakeImageSource(1280, 720);
        ScalingImageSource scalingSource = new ScalingImageSource(source, 720);

        Bitmap result = scalingSource.Capture();

        Assert.Same(source.LastCapturedBitmap, result);
        Assert.Equal(1280, result.Width);
        Assert.Equal(720, result.Height);

        result.Dispose();
        scalingSource.Dispose();
    }
}
