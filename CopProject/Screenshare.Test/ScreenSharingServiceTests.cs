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

    // 1280x720 is 20x12 = 240 tiles, so one tile is about 0.44% of the frame
    private const int WIDE = 1280;
    private const int TALL = 720;

    [Fact]
    public void ProcessFrame_TinyChange_IsHeldBackAsUnchanged()
    {
        using var frame1 = new Bitmap(WIDE, TALL);
        using var frame2 = new Bitmap(WIDE, TALL);
        frame2.SetPixel(10, 10, Color.Red);

        using var source = new FakeImageSource(frame1, frame2);
        using var service = new ScreenSharingService(source, new ImageDiffer());

        using ImageDiffResult first = service.ProcessFrame();
        using ImageDiffResult second = service.ProcessFrame();

        Assert.True(second.IsUnchanged);
    }

    [Fact]
    public void ProcessFrame_HeldChanges_PileUpAndAreSentTogether()
    {
        using var frame1 = new Bitmap(WIDE, TALL);

        // one tile changed: held back
        using var frame2 = new Bitmap(WIDE, TALL);
        frame2.SetPixel(10, 10, Color.Red);

        // two more tiles changed. against frame2 that is still small
        // the last sent frame (frame1) it is three tiles, which is over the threshold
        using var frame3 = new Bitmap(frame2);
        frame3.SetPixel(200, 10, Color.Red);
        frame3.SetPixel(400, 10, Color.Red);

        using var source = new FakeImageSource(frame1, frame2, frame3);
        using var service = new ScreenSharingService(source, new ImageDiffer());

        using ImageDiffResult first = service.ProcessFrame();
        using ImageDiffResult second = service.ProcessFrame();
        using ImageDiffResult third = service.ProcessFrame();

        Assert.True(second.IsUnchanged);
        Assert.False(third.IsUnchanged);
        Assert.False(third.IsFullFrame);
        Assert.Equal(3, third.ChangedTiles.Count);
        Assert.Contains(third.ChangedTiles, tile => tile.X == 0 && tile.Y == 0);
    }

    [Fact]
    public void ProcessFrame_HeldChange_IsSentAfterMaxHeldFrames()
    {
        using var blank = new Bitmap(WIDE, TALL);
        using var changed = new Bitmap(WIDE, TALL);
        changed.SetPixel(10, 10, Color.Red);

        var frames = new List<Bitmap> { blank };
        frames.AddRange(Enumerable.Repeat(changed, ScreenSharingService.MAX_HELD_FRAMES + 1));

        using var source = new FakeImageSource(frames.ToArray());
        using var service = new ScreenSharingService(source, new ImageDiffer());

        using ImageDiffResult first = service.ProcessFrame();

        for (int i = 0; i < ScreenSharingService.MAX_HELD_FRAMES; i++)
        {
            using ImageDiffResult held = service.ProcessFrame();
            Assert.True(held.IsUnchanged);
        }

        using ImageDiffResult sent = service.ProcessFrame();

        Assert.False(sent.IsUnchanged);
        Assert.False(sent.IsFullFrame);
        Assert.Single(sent.ChangedTiles);
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
