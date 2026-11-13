using System.Drawing;
using Xunit;

namespace Screenshare.Tests;

using ScreenShare;

public class ImageDifferTests
{
    [Fact]
    public void Compare_ReturnsUnchanged_WhenFramesAreIdentical()
    {
        IImageDiffer differ = new ImageDiffer();
        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(128, 128);

        using ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.True(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.Empty(result.ChangedTiles);

    }

    [Fact]
    public void Compare_ReturnsDelta_WhenSmallAreaChanges()
    {
        IImageDiffer differ = new ImageDiffer();
        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(128, 128);

        // change one pixel in current frame
        currentFrame.SetPixel(10, 10, Color.Red);

        using ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.NotEmpty(result.ChangedTiles);

        // the changed pixel belongs to the tile at (0,0)
        Assert.Contains(result.ChangedTiles, tile => tile.X == 0 && tile.Y == 0);
    }

    [Fact]
    public void Compare_ReturnsFullFrame_WhenResolutionChanges()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(256, 128);

        using ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.True(result.IsFullFrame);
        Assert.NotNull(result.FullFrame);
        Assert.Equal(256, result.FullFrame!.Width);
        Assert.Equal(128, result.FullFrame.Height);

        // imagediffresult does not dispose its full frame automatically
        result.FullFrame.Dispose();
    }

    [Fact]
    public void Dispose_ReleasesFullFrame()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(256, 128);

        ImageDiffResult result = differ.Compare(previousFrame, currentFrame);
        Bitmap fullFrame = result.FullFrame!;

        result.Dispose();

        // a disposed bitmap throws when its properties are read
        Assert.Throws<ArgumentException>(() => fullFrame.Width);
    }


    [Fact]
    public void Compare_HandlesPartialTileAtBottomRightEdge()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(100, 100);
        using var currentFrame = new Bitmap(100, 100);

        // change a pixel in the bottom-right parital tile
        currentFrame.SetPixel(99, 99, Color.Red);
        using ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.NotEmpty(result.ChangedTiles);

        Tile tile = Assert.Single(result.ChangedTiles);

        Assert.Equal(64, tile.X);
        Assert.Equal(64, tile.Y);
        Assert.Equal(36, tile.Width);
        Assert.Equal(36, tile.Height);
    }

    [Fact]
    public void Compare_HandlesPartialTileAtRightEdge()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(100, 128);
        using var currentFrame = new Bitmap(100, 128);

        // change a pizel in the rightmost partial tile
        currentFrame.SetPixel(99, 10, Color.Red);

        using ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.NotEmpty(result.ChangedTiles);

        Assert.Contains(result.ChangedTiles, tile => tile.X == 64 && tile.Y == 0 && tile.Width == 36 && tile.Height == 64);
    }

    [Fact]
    public void Compare_ReturnsFullFrame_WhenExactlyHalfTheTilesChange()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(128, 128);

        // A 128x128 image contains four 64x64 tiles.
        // Changing two tiles means exactly 50% of the image is changed.
        currentFrame.SetPixel(10, 10, Color.Red);
        currentFrame.SetPixel(70, 10, Color.Red);

        using ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.True(result.IsFullFrame);
        Assert.Empty(result.ChangedTiles);

        Assert.NotNull(result.FullFrame);
        Assert.Equal(128, result.FullFrame.Width);
        Assert.Equal(128, result.FullFrame.Height);
    }

    [Fact]
    public void Compare_ReturnsFullFrame_WhenMoreThanHalfTheTilesChange()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(128, 128);

        // Three out of four tiles change (75%).
        currentFrame.SetPixel(10, 10, Color.Red);
        currentFrame.SetPixel(70, 10, Color.Red);
        currentFrame.SetPixel(10, 70, Color.Red);

        using ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.True(result.IsFullFrame);
        Assert.Empty(result.ChangedTiles);
        Assert.NotNull(result.FullFrame);
    }

    [Fact]
    public void Compare_ReturnsMultipleChangedTiles_WhenChangeIsBelowThreshold()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(256, 256);
        using var currentFrame = new Bitmap(256, 256);

        // Change one pixel in each of three different tiles.
        // Each tile is 64x64, so the changed area is 3/16 = 18.75%.
        currentFrame.SetPixel(10, 10, Color.Red);
        currentFrame.SetPixel(70, 10, Color.Red);
        currentFrame.SetPixel(130, 10, Color.Red);

        using ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.Null(result.FullFrame);
        Assert.Equal(3, result.ChangedTiles.Count);

        Assert.Contains(result.ChangedTiles, tile => tile.X == 0 && tile.Y == 0);
        Assert.Contains(result.ChangedTiles, tile => tile.X == 64 && tile.Y == 0);
        Assert.Contains(result.ChangedTiles, tile => tile.X == 128 && tile.Y == 0);
    }

}
