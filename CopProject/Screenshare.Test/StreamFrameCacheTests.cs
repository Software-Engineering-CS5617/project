using System.Drawing;
using ScreenShare;

namespace Screenshare.Tests;

public class StreamFrameCacheTests
{
    [Fact]
    public void UpdateStoresSeparateFramesForDifferentStreams()
    {
        using var cache = new StreamFrameCache();
        using var blue = new Bitmap(32, 32);
        using var red = new Bitmap(32, 32);

        using (Graphics g = Graphics.FromImage(blue))
            g.Clear(Color.Blue);

        using (Graphics g = Graphics.FromImage(red))
            g.Clear(Color.Red);

        cache.Update("stream-A", blue);
        cache.Update("stream-B", red);

        using Bitmap? frameA = cache.Get("stream-A");
        using Bitmap? frameB = cache.Get("stream-B");

        Assert.NotNull(frameA);
        Assert.NotNull(frameB);

        Assert.Equal(
            Color.Blue.ToArgb(),
            frameA.GetPixel(0, 0).ToArgb());

        Assert.Equal(
            Color.Red.ToArgb(),
            frameB.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void UpdateReplacesOnlyTheSpecifiedStream()
    {
        using var cache = new StreamFrameCache();
        using var blue = new Bitmap(32, 32);
        using var red = new Bitmap(32, 32);
        using var green = new Bitmap(32, 32);

        using (Graphics g = Graphics.FromImage(blue))
            g.Clear(Color.Blue);

        using (Graphics g = Graphics.FromImage(red))
            g.Clear(Color.Red);

        using (Graphics g = Graphics.FromImage(green))
            g.Clear(Color.Green);

        cache.Update("stream-A", blue);
        cache.Update("stream-B", red);

        cache.Update("stream-A", green);

        using Bitmap? frameA = cache.Get("stream-A");
        using Bitmap? frameB = cache.Get("stream-B");

        Assert.NotNull(frameA);
        Assert.NotNull(frameB);

        Assert.Equal(
            Color.Green.ToArgb(),
            frameA.GetPixel(0, 0).ToArgb());

        Assert.Equal(
            Color.Red.ToArgb(),
            frameB.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void GetReturnsNullForUnknownStream()
    {
        using var cache = new StreamFrameCache();

        Assert.Null(cache.Get("unknown-stream"));
    }

    [Fact]
    public void RemoveDeletesOnlyTheRequestedStream()
    {
        using var cache = new StreamFrameCache();
        using var blue = new Bitmap(32, 32);
        using var red = new Bitmap(32, 32);

        using (Graphics g = Graphics.FromImage(blue))
            g.Clear(Color.Blue);

        using (Graphics g = Graphics.FromImage(red))
            g.Clear(Color.Red);

        cache.Update("stream-A", blue);
        cache.Update("stream-B", red);

        cache.Remove("stream-A");

        Assert.Null(cache.Get("stream-A"));

        using Bitmap? frameB = cache.Get("stream-B");

        Assert.NotNull(frameB);
        Assert.Equal(
            Color.Red.ToArgb(),
            frameB.GetPixel(0, 0).ToArgb());
    }
}
