using System.Drawing;
using System.Drawing.Imaging;
using ScreenShare;
using Xunit;

namespace Screenshare.Tests;

public class ScreenPacketApplierTests
{
    private const string StreamId = "alice-screen";
    private static readonly DateTime s_captureTime = new(2026, 11, 3, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Apply_FullFrame_UpdatesStitcher()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);

        Assert.Equal(PacketApplyOutcome.Applied, applier.Apply(FullFramePacket(0, Color.Blue)));

        using Bitmap? frame = stitcher.GetCurrentFrame();
        Assert.Equal(Color.Blue.ToArgb(), frame!.GetPixel(10, 10).ToArgb());
    }

    [Fact]
    public void Apply_DeltaAndPulseAfterFullFrame_AreApplied()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);
        applier.Apply(FullFramePacket(0, Color.Blue));

        Assert.Equal(PacketApplyOutcome.Applied, applier.Apply(TilePacket(1, Color.Red)));
        Assert.Equal(PacketApplyOutcome.Applied, applier.Apply(PulsePacket(2)));

        using Bitmap? frame = stitcher.GetCurrentFrame();
        Assert.Equal(Color.Red.ToArgb(), frame!.GetPixel(10, 10).ToArgb());
        Assert.Equal(2, stitcher.LastFrameNumber);
    }

    [Fact]
    public void Apply_DeltaBeforeFullFrame_RequestsFullFrame()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);
        var requests = new List<string>();
        applier.FullFrameRequested += requests.Add;

        Assert.Equal(PacketApplyOutcome.NeedsFullFrame, applier.Apply(TilePacket(5, Color.Red)));
        Assert.Equal(new[] { StreamId }, requests);
    }

    [Fact]
    public void Apply_RateLimitsFullFrameRequests()
    {
        using var stitcher = new ImageStitcher();
        var clock = new ManualTimeProvider();
        var applier = new ScreenPacketApplier(StreamId, stitcher, clock);
        int requests = 0;
        applier.FullFrameRequested += _ => requests++;

        applier.Apply(PulsePacket(1));
        applier.Apply(PulsePacket(2));
        clock.Advance(ScreenPacketApplier.FullFrameRequestInterval - TimeSpan.FromMilliseconds(1));
        applier.Apply(PulsePacket(3));

        Assert.Equal(1, requests);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        applier.Apply(PulsePacket(4));

        Assert.Equal(2, requests);
    }

    [Fact]
    public void Apply_GapAfterFullFrame_RequestsFullFrame()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);
        int requests = 0;
        applier.FullFrameRequested += _ => requests++;
        applier.Apply(FullFramePacket(0, Color.Blue));

        Assert.Equal(PacketApplyOutcome.NeedsFullFrame, applier.Apply(PulsePacket(2)));
        Assert.Equal(1, requests);
    }

    [Fact]
    public void Apply_StalePacket_IsIgnoredWithoutRequest()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);
        int requests = 0;
        applier.FullFrameRequested += _ => requests++;
        applier.Apply(FullFramePacket(3, Color.Blue));

        Assert.Equal(PacketApplyOutcome.Ignored, applier.Apply(PulsePacket(3)));
        Assert.Equal(PacketApplyOutcome.Ignored, applier.Apply(FullFramePacket(2, Color.Red)));
        Assert.Equal(0, requests);
    }

    [Fact]
    public void Apply_CorruptFullFrame_IsDroppedAndStillNeedsFullFrame()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);
        var packet = new ScreenDataPacket(
            StreamId, 0, s_captureTime, 128, 128, DataPacketType.FullFrame,
            fullFrameData: new byte[] { 1, 2, 3 });

        Assert.Equal(PacketApplyOutcome.NeedsFullFrame, applier.Apply(packet));
        Assert.Null(stitcher.GetCurrentFrame());
    }

    [Fact]
    public void Apply_FullFrameWithWrongSize_IsDropped()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);

        // header says 128x128, image is 64x64
        var packet = new ScreenDataPacket(
            StreamId, 0, s_captureTime, 128, 128, DataPacketType.FullFrame,
            fullFrameData: Png(64, 64, Color.Blue));

        Assert.Equal(PacketApplyOutcome.NeedsFullFrame, applier.Apply(packet));
        Assert.Null(stitcher.GetCurrentFrame());
    }

    [Fact]
    public void Apply_CorruptTile_IsDroppedAndNextPacketSeesGap()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);
        applier.Apply(FullFramePacket(0, Color.Blue));

        var corrupt = new ScreenDataPacket(
            StreamId, 1, s_captureTime, 128, 128, DataPacketType.ChangedTiles,
            changedTiles: new[] { new Tile(0, 0, 64, 64, 0, new byte[] { 1, 2, 3 }) });

        Assert.Equal(PacketApplyOutcome.Ignored, applier.Apply(corrupt));
        Assert.Equal(PacketApplyOutcome.NeedsFullFrame, applier.Apply(PulsePacket(2)));
    }

    [Fact]
    public void Apply_PacketFromOtherStream_Throws()
    {
        using var stitcher = new ImageStitcher();
        var applier = new ScreenPacketApplier(StreamId, stitcher);
        var packet = new ScreenDataPacket("bob-screen", 0, s_captureTime, 128, 128, DataPacketType.Pulse);

        Assert.Throws<ArgumentException>(() => applier.Apply(packet));
    }

    private static ScreenDataPacket FullFramePacket(long frameNumber, Color color)
    {
        return new(StreamId, frameNumber, s_captureTime, 128, 128, DataPacketType.FullFrame,
            fullFrameData: Png(128, 128, color));
    }

    private static ScreenDataPacket TilePacket(long frameNumber, Color color)
    {
        return new(StreamId, frameNumber, s_captureTime, 128, 128, DataPacketType.ChangedTiles,
            changedTiles: new[] { new Tile(0, 0, 64, 64, 0, Png(64, 64, color)) });
    }

    private static ScreenDataPacket PulsePacket(long frameNumber)
    {
        return new(StreamId, frameNumber, s_captureTime, 128, 128, DataPacketType.Pulse);
    }

    private static byte[] Png(int width, int height, Color color)
    {
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(color);
        }

        // PngEncoder rather than Save(stream, ImageFormat.Png), which can fail when tests run in parallel
        return PngEncoder.Encode(bitmap);
    }

    /// <summary>
    /// a clock that only moves when the test advances it
    /// </summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 3, 10, 30, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public void Advance(TimeSpan by)
        {
            _now += by;
        }
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenStreamIdIsBlank()
    {
        ImageStitcher stitcher = new ImageStitcher();

        Assert.Throws<ArgumentException>(() => new ScreenPacketApplier(" ", stitcher));

        stitcher.Dispose();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenStitcherIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenPacketApplier(StreamId, null!));
    }

    [Fact]
    public void Apply_ThrowsArgumentNullException_WhenPacketIsNull()
    {
        ImageStitcher stitcher = new ImageStitcher();
        ScreenPacketApplier applier = new ScreenPacketApplier(StreamId, stitcher);

        Assert.Throws<ArgumentNullException>(() => applier.Apply(null!));

        stitcher.Dispose();
    }
}
