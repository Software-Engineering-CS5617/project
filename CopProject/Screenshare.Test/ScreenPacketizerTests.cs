using System.Drawing;
using ScreenShare;
using Xunit;

namespace Screenshare.Tests;

public class ScreenPacketizerTests
{
    private static readonly DateTime s_captureTime = new(2026, 11, 10, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    private void CreatePacket_FullFrame_EncodesDecodablePng()
    {
        ScreenPacketizer packetizer = new ScreenPacketizer("sanjays-screen");
        using Bitmap frame = new Bitmap(200, 100);
        frame.SetPixel(5, 5, Color.Red);
        using ImageDiffResult result = ImageDiffResult.CreateFullFrame(new Bitmap(frame));

        ScreenDataPacket packet = packetizer.CreatePacket(result, s_captureTime);

        Assert.True(packet.IsFullFrame);
        Assert.Equal("sanjays-screen", packet.StreamId);
        Assert.Equal(0, packet.FrameNumber);
        Assert.Equal(s_captureTime, packet.Timestamp);
        Assert.Equal(200, packet.Width);
        Assert.Equal(100, packet.Height);

        using MemoryStream stream = new MemoryStream(packet.FullFrameData!);
        using Bitmap decoded = new Bitmap(stream);
        Assert.Equal(200, decoded.Width);
        Assert.Equal(Color.Red.ToArgb(), decoded.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void CreatePacket_Unchanged_BecomesPulseWithLastFrameSize()
    {
        ScreenPacketizer packetizer = new ScreenPacketizer("sanjays-screen");
        using ImageDiffResult full = ImageDiffResult.CreateFullFrame(new Bitmap(200, 100));
        using ImageDiffResult unchanged = ImageDiffResult.Unchanged();

        packetizer.CreatePacket(full, s_captureTime);
        ScreenDataPacket packet = packetizer.CreatePacket(unchanged, s_captureTime);

        Assert.True(packet.IsPulse);
        Assert.Equal(1, packet.FrameNumber);
        Assert.Equal(200, packet.Width);
        Assert.Equal(100, packet.Height);
    }

    [Fact]
    public void CreatePacket_Delta_KeepsTiles()
    {
        ScreenPacketizer packetizer = new ScreenPacketizer("sanjays-screen");
        using ImageDiffResult full = ImageDiffResult.CreateFullFrame(new Bitmap(128, 128));
        var tile = new Tile(64, 0, 64, 64, 1, new byte[] { 1 });
        using ImageDiffResult delta = ImageDiffResult.Delta(new List<Tile> { tile });

        packetizer.CreatePacket(full, s_captureTime);
        ScreenDataPacket packet = packetizer.CreatePacket(delta, s_captureTime);

        Assert.Equal(DataPacketType.ChangedTiles, packet.Type);
        Assert.Same(tile, Assert.Single(packet.ChangedTiles));
    }

    [Fact]
    public void CreatePacket_PulseBeforeFullFrame_ThrowsAndKeepsFrameNumber()
    {
        ScreenPacketizer packetizer = new ScreenPacketizer("sanjays-screen");
        using ImageDiffResult unchanged = ImageDiffResult.Unchanged();

        Assert.Throws<InvalidOperationException>(() => packetizer.CreatePacket(unchanged, s_captureTime));
        Assert.Equal(0, packetizer.NextFrameNumber);
    }

    [Fact]
    public void CreatePacket_DeltaBeforeFullFrame_Throws()
    {
        ScreenPacketizer packetizer = new ScreenPacketizer("sanjays-screen");
        var tile = new Tile(0, 0, 64, 64, 1, new byte[] { 1 });
        using ImageDiffResult delta = ImageDiffResult.Delta(new List<Tile> { tile });

        Assert.Throws<InvalidOperationException>(() => packetizer.CreatePacket(delta, s_captureTime));
    }

    [Fact]
    public void CreatePacket_NullResult_ThrowsArgumentNullException()
    {
        ScreenPacketizer packetizer = new ScreenPacketizer("sanjays-screen");

        Assert.Throws<ArgumentNullException>(() => packetizer.CreatePacket(null!, s_captureTime));
    }
}
