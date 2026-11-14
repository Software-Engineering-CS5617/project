using System.Drawing;
using System.Drawing.Imaging;
using ScreenShare;

namespace Screenshare.Tests;

public class ImageStitcherTests
{
    [Fact]
    public void ApplyFullFrame_StoresCopyOfFrame()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);

        Assert.True(stitcher.ApplyFullFrame(0, original));

        using Bitmap? result = stitcher.GetCurrentFrame();

        Assert.NotNull(result);
        Assert.Equal(128, result.Width);
        Assert.Equal(128, result.Height);
        Assert.Equal(Color.Blue.ToArgb(),
            result.GetPixel(10, 10).ToArgb());
        Assert.Equal(0, stitcher.LastFrameNumber);
        Assert.False(stitcher.NeedsFullFrame);
    }

    [Fact]
    public void ApplyChangedTiles_UpdatesOnlySpecifiedRegion()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);

        stitcher.ApplyFullFrame(0, original);

        Tile tile = SolidTile(0, 0, 64, 64, Color.Red);

        Assert.True(stitcher.ApplyChangedTiles(1, new[] { tile }));

        using Bitmap? result = stitcher.GetCurrentFrame();

        Assert.NotNull(result);

        // changed region should be red.
        Assert.Equal(Color.Red.ToArgb(),
            result.GetPixel(10, 10).ToArgb());

        // unchanged region should remain blue.
        Assert.Equal(Color.Blue.ToArgb(),
            result.GetPixel(100, 100).ToArgb());
    }

    [Fact]
    public void ApplyChangedTiles_ReplacesPixelsInsteadOfBlending()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);

        stitcher.ApplyFullFrame(0, original);

        // half-transparent red; blending over blue would give a purple pixel
        Color translucentRed = Color.FromArgb(128, 255, 0, 0);
        Tile tile = SolidTile(0, 0, 64, 64, translucentRed);

        stitcher.ApplyChangedTiles(1, new[] { tile });

        using Bitmap? result = stitcher.GetCurrentFrame();

        Assert.NotNull(result);
        Assert.Equal(translucentRed.ToArgb(), result.GetPixel(10, 10).ToArgb());
    }

    [Fact]
    public void ApplyChangedTiles_ThrowsWhenTileIsOutsideFrame()
    {
        using var stitcher = new ImageStitcher();
        using var frame = new Bitmap(128, 128);

        stitcher.ApplyFullFrame(0, frame);

        Tile tile = SolidTile(100, 100, 64, 64, Color.Red);

        Assert.Throws<ArgumentException>(
            () => stitcher.ApplyChangedTiles(1, new[] { tile })
            );
    }

    [Fact]
    public void ApplyChangedTiles_ThrowsWhenEncodedDimensionsDoNotMatch()
    {
        using var stitcher = new ImageStitcher();
        using var frame = new Bitmap(128, 128);
        stitcher.ApplyFullFrame(0, frame);

        // encoded image is 32x32 , but metadata says 64x64
        using var tileBitmap = new Bitmap(32, 32);

        var tile = new Tile(
            x: 0,
            y: 0,
            width: 64,
            height: 64,
            hash: 0,
            data: PngEncoder.Encode(tileBitmap)
            );
        Assert.Throws<ArgumentException>(() => stitcher.ApplyChangedTiles(1, new[] { tile }));
    }

    [Fact]
    public void ApplyChangedTiles_InvalidTile_LeavesFrameAndFrameNumberUnchanged()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);
        stitcher.ApplyFullFrame(0, original);

        Tile goodTile = SolidTile(0, 0, 64, 64, Color.Red);
        Tile badTile = SolidTile(100, 100, 64, 64, Color.Red);

        Assert.Throws<ArgumentException>(
            () => stitcher.ApplyChangedTiles(1, new[] { goodTile, badTile }));

        using Bitmap? result = stitcher.GetCurrentFrame();

        Assert.NotNull(result);
        Assert.Equal(Color.Blue.ToArgb(), result.GetPixel(10, 10).ToArgb());
        Assert.Equal(0, stitcher.LastFrameNumber);
    }

    [Fact]
    public void ApplyChangedTiles_IgnoredBeforeFirstFullFrame()
    {
        using var stitcher = new ImageStitcher();

        // a receiver joining mid-stream gets deltas before its first full frame
        Tile tile = SolidTile(0, 0, 64, 64, Color.Red);

        Assert.False(stitcher.ApplyChangedTiles(0, new[] { tile }));
        Assert.True(stitcher.NeedsFullFrame);
        Assert.Null(stitcher.GetCurrentFrame());
    }

    [Fact]
    public void ApplyChangedTiles_IgnoresDuplicateAndOlderFrames()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);
        stitcher.ApplyFullFrame(5, original);

        Tile tile = SolidTile(0, 0, 64, 64, Color.Red);

        Assert.False(stitcher.ApplyChangedTiles(5, new[] { tile }));
        Assert.False(stitcher.ApplyChangedTiles(3, new[] { tile }));

        using Bitmap? result = stitcher.GetCurrentFrame();

        Assert.Equal(Color.Blue.ToArgb(), result!.GetPixel(10, 10).ToArgb());
        Assert.Equal(5, stitcher.LastFrameNumber);
        Assert.False(stitcher.NeedsFullFrame);
    }

    [Fact]
    public void ApplyChangedTiles_GapRequiresFullFrameAndDropsLaterDeltas()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);
        stitcher.ApplyFullFrame(0, original);

        Tile tile = SolidTile(0, 0, 64, 64, Color.Red);

        // frame 1 was lost
        Assert.False(stitcher.ApplyChangedTiles(2, new[] { tile }));
        Assert.True(stitcher.NeedsFullFrame);

        // even the late frame 1 and the following frame 3 are dropped
        Assert.False(stitcher.ApplyChangedTiles(1, new[] { tile }));
        Assert.False(stitcher.ApplyChangedTiles(3, new[] { tile }));
        Assert.False(stitcher.ApplyPulse(4));

        using Bitmap? result = stitcher.GetCurrentFrame();
        Assert.Equal(Color.Blue.ToArgb(), result!.GetPixel(10, 10).ToArgb());
        Assert.Equal(0, stitcher.LastFrameNumber);
    }

    [Fact]
    public void ApplyFullFrame_RecoversAfterGap()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);
        using Bitmap recovery = SolidBitmap(128, 128, Color.Green);
        stitcher.ApplyFullFrame(0, original);

        Tile tile = SolidTile(0, 0, 64, 64, Color.Red);
        stitcher.ApplyChangedTiles(2, new[] { tile });

        Assert.True(stitcher.ApplyFullFrame(6, recovery));
        Assert.False(stitcher.NeedsFullFrame);

        Assert.True(stitcher.ApplyChangedTiles(7, new[] { tile }));

        using Bitmap? result = stitcher.GetCurrentFrame();
        Assert.Equal(Color.Red.ToArgb(), result!.GetPixel(10, 10).ToArgb());
        Assert.Equal(Color.Green.ToArgb(), result.GetPixel(100, 100).ToArgb());
        Assert.Equal(7, stitcher.LastFrameNumber);
    }

    [Fact]
    public void ApplyFullFrame_IgnoresStaleFrame()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap newer = SolidBitmap(128, 128, Color.Blue);
        using Bitmap older = SolidBitmap(128, 128, Color.Red);

        stitcher.ApplyFullFrame(10, newer);

        Assert.False(stitcher.ApplyFullFrame(9, older));
        Assert.False(stitcher.ApplyFullFrame(10, older));

        using Bitmap? result = stitcher.GetCurrentFrame();
        Assert.Equal(Color.Blue.ToArgb(), result!.GetPixel(10, 10).ToArgb());
    }

    [Fact]
    public void ApplyPulse_AdvancesFrameNumberAndKeepsFrame()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);
        stitcher.ApplyFullFrame(0, original);

        Assert.True(stitcher.ApplyPulse(1));
        Assert.True(stitcher.ApplyPulse(2));

        // a delta after the pulses continues the sequence
        Tile tile = SolidTile(0, 0, 64, 64, Color.Red);
        Assert.True(stitcher.ApplyChangedTiles(3, new[] { tile }));

        Assert.Equal(3, stitcher.LastFrameNumber);
    }

    [Fact]
    public void Reset_RequiresNewFullFrame()
    {
        using var stitcher = new ImageStitcher();
        using Bitmap original = SolidBitmap(128, 128, Color.Blue);
        stitcher.ApplyFullFrame(4, original);

        stitcher.Reset();

        Assert.Null(stitcher.GetCurrentFrame());
        Assert.True(stitcher.NeedsFullFrame);
        Assert.Equal(-1, stitcher.LastFrameNumber);

        // a restarted stream may begin numbering from 0 again
        Assert.True(stitcher.ApplyFullFrame(0, original));
    }

    private static Bitmap SolidBitmap(int width, int height, Color color)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(color);
        }

        return bitmap;
    }

    private static Tile SolidTile(int x, int y, int width, int height, Color color)
    {
        using Bitmap tileBitmap = SolidBitmap(width, height, color);
        // PngEncoder rather than Save(stream, ImageFormat.Png), which can fail when tests run in parallel
        return new Tile(x, y, width, height, hash: 0, data: PngEncoder.Encode(tileBitmap));

    }

    [Fact]
    public void ApplyFullFrame_ThrowsArgumentNullException_WhenFrameIsNull()
    {
        using var stitcher = new ImageStitcher();

        Assert.Throws<ArgumentNullException>(() => stitcher.ApplyFullFrame(0, null!));
    }

    [Fact]
    public void ApplyFullFrame_ThrowsArgumentOutOfRangeException_WhenFrameNumberIsNegative()
    {
        using var stitcher = new ImageStitcher();
        using var frame = new Bitmap(128, 128);

        Assert.Throws<ArgumentOutOfRangeException>(() => stitcher.ApplyFullFrame(-1, frame));
    }

    [Fact]
    public void ApplyChangedTiles_ThrowsArgumentException_WhenTileListIsEmpty()
    {
        using var stitcher = new ImageStitcher();
        using var frame = new Bitmap(128, 128);

        stitcher.ApplyFullFrame(0, frame);

        Assert.Throws<ArgumentException>(() => stitcher.ApplyChangedTiles(1, Array.Empty<Tile>()));

        Assert.Equal(0, stitcher.LastFrameNumber);
    }

    [Fact]
    public void ApplyChangedTiles_ThrowsArgumentNullException_WhenTileIsNull()
    {
        using var stitcher = new ImageStitcher();
        using var frame = new Bitmap(128, 128);

        stitcher.ApplyFullFrame(0, frame);

        Tile[] tiles = { null! };

        Assert.Throws<ArgumentNullException>(() => stitcher.ApplyChangedTiles(1, tiles));

        Assert.Equal(0, stitcher.LastFrameNumber);
    }

    [Fact]
    public void ApplyPulse_ThrowsArgumentOutOfRangeException_WhenFrameNumberIsNegative()
    {
        using var stitcher = new ImageStitcher();

        Assert.Throws<ArgumentOutOfRangeException>(() => stitcher.ApplyPulse(-1));
    }
}
