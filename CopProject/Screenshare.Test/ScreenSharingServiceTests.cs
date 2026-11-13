using System.Drawing;
using ScreenShare;
using Xunit;

namespace Screenshare.Tests;

public class ScreenSharingServiceTests
{
    [Fact]
    public void ProcessFrame_FirstFrame_ReturnsFullFrame()
    {
        using var frame = new Bitmap(128, 128);
        using var source = new FakeImageSource(frame);
        IImageDiffer differ = new ImageDiffer();

        using var service = new ScreenSharingService(source, differ);

        using ImageDiffResult result = service.ProcessFrame();

        Assert.True(result.IsFullFrame);
        Assert.False(result.IsUnchanged);
        Assert.NotNull(result.FullFrame);

        Assert.Equal(128, result.FullFrame!.Width);
        Assert.Equal(128, result.FullFrame.Height);

    }

    [Fact]
    public void ProcessFrame_UnchangedFrame_ReturnsUnchanged()
    {
        using var frame1 = new Bitmap(128, 128);
        using var frame2 = new Bitmap(128, 128);

        using var source = new FakeImageSource(frame1, frame2);
        IImageDiffer differ = new ImageDiffer();

        using var service = new ScreenSharingService(source, differ);

        using ImageDiffResult firstResult = service.ProcessFrame();
        using ImageDiffResult secondResult = service.ProcessFrame();

        Assert.True(firstResult.IsFullFrame);
        Assert.True(secondResult.IsUnchanged);
        Assert.False(secondResult.IsFullFrame);
    }

    [Fact]
    public void ProcessFrame_ChangedFrame_ReturnsDelta()
    {
        using var frame1 = new Bitmap(128, 128);
        using var frame2 = new Bitmap(128, 128);

        frame2.SetPixel(10, 10, Color.Red);

        using var source = new FakeImageSource(frame1, frame2);
        IImageDiffer differ = new ImageDiffer();

        using var service = new ScreenSharingService(source, differ);

        using ImageDiffResult firstResult = service.ProcessFrame();
        using ImageDiffResult secondResult = service.ProcessFrame();

        Assert.True(firstResult.IsFullFrame);
        Assert.False(secondResult.IsUnchanged);
        Assert.False(secondResult.IsFullFrame);
        Assert.NotEmpty(secondResult.ChangedTiles);


    }

    [Fact]
    public void Reset_NextFrame_ReturnsFullFrame()
    {
        using var frame1 = new Bitmap(128, 128);
        using var frame2 = new Bitmap(128, 128);

        using var source = new FakeImageSource(frame1, frame2);
        IImageDiffer differ = new ImageDiffer();

        using var service = new ScreenSharingService(source, differ);

        using ImageDiffResult firstResult = service.ProcessFrame();
        //firstResult.FullFrame?.Dispose();

        service.Reset();

        using ImageDiffResult resultAfterReset = service.ProcessFrame();

        Assert.True(resultAfterReset.IsFullFrame);
        Assert.False(resultAfterReset.IsUnchanged);
        Assert.NotNull(resultAfterReset.FullFrame);

        //resultAfterReset.FullFrame?.Dispose();
    }

    [Fact]
    public void ProcessFrame_AfterDispose_ThrowsObjectDisposedException()
    {
        using var frame = new Bitmap(128, 128);
        using var source = new FakeImageSource(frame);
        IImageDiffer differ = new ImageDiffer();

        var service = new ScreenSharingService(source, differ);

        service.Dispose();

        Assert.Throws<ObjectDisposedException>(service.ProcessFrame);
    }

    /// <summary>
    /// supplies predetermined frames for testing without capturing the desktop.
    /// </summary>
    private sealed class FakeImageSource : IImageSource, IDisposable
    {
        private readonly Queue<Bitmap> _frames = new();

        public FakeImageSource(params Bitmap[] frames)
        {
            foreach (Bitmap frame in frames)
            {
                _frames.Enqueue(new Bitmap(frame));
            }
        }

        public Bitmap Capture()
        {
            if (_frames.Count == 0)
            {
                throw new InvalidOperationException(
                    "No test frames remain.");
            }

            using Bitmap storedFrame = _frames.Dequeue();
            return new Bitmap(storedFrame);
        }

        public void Dispose()
        {
            while (_frames.Count > 0)
            {
                _frames.Dequeue().Dispose();
            }
        }
    }
}
