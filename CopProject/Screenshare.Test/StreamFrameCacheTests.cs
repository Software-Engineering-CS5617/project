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
        {
            g.Clear(Color.Blue);
        }

        using (Graphics g = Graphics.FromImage(red))
        {
            g.Clear(Color.Red);
        }

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
        {
            g.Clear(Color.Blue);
        }

        using (Graphics g = Graphics.FromImage(red))
        {
            g.Clear(Color.Red);
        }

        using (Graphics g = Graphics.FromImage(green))
        {
            g.Clear(Color.Green);
        }

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
        {
            g.Clear(Color.Blue);
        }

        using (Graphics g = Graphics.FromImage(red))
        {
            g.Clear(Color.Red);
        }

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

    [Fact]
    public void UpdateThrowsArgumentException_WhenStreamIdIsBlank()
    {
        using var cache = new StreamFrameCache();
        using var frame = new Bitmap(32, 32);

        Assert.Throws<ArgumentException>(() => cache.Update(" ", frame));
    }

    [Fact]
    public void UpdateThrowsArgumentNullException_WhenFrameIsNull()
    {
        using var cache = new StreamFrameCache();

        Assert.Throws<ArgumentNullException>(() => cache.Update("stream-A", null!));
    }

    [Fact]
    public void GetThrowsArgumentException_WhenStreamIdIsBlank()
    {
        using var cache = new StreamFrameCache();

        Assert.Throws<ArgumentException>(() => cache.Get(""));
    }

    [Fact]
    public void RemoveThrowsArgumentException_WhenStreamIdIsBlank()
    {
        using var cache = new StreamFrameCache();

        Assert.Throws<ArgumentException>(() => cache.Remove(" "));
    }

    [Fact]
    public void GetReturnsIndependentCopy_OfCachedFrame()
    {
        using var cache = new StreamFrameCache();
        using var original = new Bitmap(32, 32);

        original.SetPixel(0, 0, Color.Blue);
        cache.Update("stream-A", original);

        using Bitmap? retrieved = cache.Get("stream-A");
        Assert.NotNull(retrieved);

        retrieved.SetPixel(0, 0, Color.Red);

        using Bitmap? cached = cache.Get("stream-A");

        Assert.NotNull(cached);
        Assert.Equal(Color.Blue.ToArgb(), cached.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void UpdateThrowsObjectDisposedException_AfterDisposal()
    {
        var cache = new StreamFrameCache();
        cache.Dispose();

        using var frame = new Bitmap(32, 32);

        Assert.Throws<ObjectDisposedException>(() => cache.Update("stream-A", frame));
    }

    [Fact]
    public void GetThrowsObjectDisposedException_AfterDisposal()
    {
        var cache = new StreamFrameCache();
        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cache.Get("stream-A"));
    }

    [Fact]
    public void RemoveThrowsObjectDisposedException_AfterDisposal()
    {
        var cache = new StreamFrameCache();
        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cache.Remove("stream-A"));
    }

    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        var cache = new StreamFrameCache();

        cache.Dispose();
        cache.Dispose();
    }
}
