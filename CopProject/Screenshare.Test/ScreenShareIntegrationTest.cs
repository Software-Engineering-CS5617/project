using System.Drawing;
using ScreenShare;
using Xunit;

namespace Screenshare.Tests;

/// <summary>
/// Tests the complete diffing and stitching pipeline
/// </summary>
public class ScreenShareIntegrationTests
{
    [Fact]
    public void DiffAndStitch_ReconstructsModifiedFrameCorrectly()
    {
        var differ = new ImageDiffer();
        using var stitcher = new ImageStitcher();

        // 1. Create the initial screen frame.
        using var originalFrame = new Bitmap(256, 256);

        using (Graphics graphics = Graphics.FromImage(originalFrame))
        {
            graphics.Clear(Color.White);
        }

        // 2. Initialize the receiver with the original full frame.
        stitcher.ApplyFullFrame(0, originalFrame);

        // 3. Create the next frame with changes in two different tiles.
        using var modifiedFrame = new Bitmap(originalFrame);

        modifiedFrame.SetPixel(10, 10, Color.Red);
        modifiedFrame.SetPixel(100, 100, Color.Blue);

        // 4. Generate the delta that would be transmitted over the network.
        using ImageDiffResult result = differ.Compare(
            originalFrame,
            modifiedFrame);

        Assert.False(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.Equal(2, result.ChangedTiles.Count);

        // 5. Apply the transmitted tiles at the receiver.
        Assert.True(stitcher.ApplyChangedTiles(1, result.ChangedTiles));


        // 6. Retrieve the reconstructed screen.
        using Bitmap? reconstructedFrame = stitcher.GetCurrentFrame();

        Assert.NotNull(reconstructedFrame);
        Assert.Equal(modifiedFrame.Width, reconstructedFrame.Width);
        Assert.Equal(modifiedFrame.Height, reconstructedFrame.Height);

        // 7. Verify every pixel matches the source frame.
        for (int y = 0; y < modifiedFrame.Height; y++)
        {
            for (int x = 0; x < modifiedFrame.Width; x++)
            {
                Assert.Equal(
                    modifiedFrame.GetPixel(x, y).ToArgb(),
                    reconstructedFrame.GetPixel(x, y).ToArgb());
            }
        }
    }

    [Fact]
    public void DiffAndStitch_LargeChange_UsesFullFrameAndReconstructsCorrectly()
    {
        var differ = new ImageDiffer();
        using var stitcher = new ImageStitcher();

        using var originalFrame = new Bitmap(128, 128);
        using var modifiedFrame = new Bitmap(128, 128);

        using (Graphics graphics = Graphics.FromImage(originalFrame))
        {
            graphics.Clear(Color.White);
        }

        using (Graphics graphics = Graphics.FromImage(modifiedFrame))
        {
            graphics.Clear(Color.Black);
        }

        // Initialize the receiver with the original frame.
        stitcher.ApplyFullFrame(0, originalFrame);

        // Compare the original and modified frames.
        using ImageDiffResult result = differ.Compare(originalFrame, modifiedFrame);

        // The entire frame changed, so a full frame should be sent.
        Assert.True(result.IsFullFrame);
        Assert.False(result.IsUnchanged);
        Assert.NotNull(result.FullFrame);

        // Simulate the receiver applying the full-frame packet.
        Assert.True(stitcher.ApplyFullFrame(1, result.FullFrame!));

        using Bitmap? reconstructedFrame = stitcher.GetCurrentFrame();

        Assert.NotNull(reconstructedFrame);

        // Verify that the reconstructed image matches the modified frame.
        for (int y = 0; y < modifiedFrame.Height; y++)
        {
            for (int x = 0; x < modifiedFrame.Width; x++)
            {
                Assert.Equal(
                    modifiedFrame.GetPixel(x, y).ToArgb(),
                    reconstructedFrame.GetPixel(x, y).ToArgb());
            }
        }

        result.FullFrame.Dispose();
    }
}
