using ScreenShare;
using Xunit;

namespace Screenshare.Tests;

/// <summary>
/// tests validation and data storage in ScreenDataPacket
/// </summary>

public class ScreenDataPacketTests
{

    private static readonly DateTime s_captureTime = new(2026, 11, 3, 10, 30, 0, DateTimeKind.Utc);

    /// <summary>
    /// verifies that a full frame packet stores its image data and metadata
    /// </summary>
    [Fact]
    public void ConstructorFullFramePacketStoresData()
    {
        byte[] imageData = { 1, 2, 3, 4 };

        ScreenDataPacket packet = new(
            "stream-A",
            10,
            s_captureTime,
            1280,
            720,
            DataPacketType.FullFrame,
            fullFrameData: imageData
            );

        Assert.Equal("stream-A", packet.StreamId);
        Assert.Equal(10, packet.FrameNumber);
        Assert.Equal(s_captureTime, packet.Timestamp);
        Assert.Equal(1280, packet.Width);
        Assert.Equal(720, packet.Height);
        Assert.Equal(ImageDiffer.TILE_SIZE, packet.TileSize);
        Assert.Equal(DataPacketType.FullFrame, packet.Type);
        Assert.True(packet.IsFullFrame);
        Assert.False(packet.IsPulse);
        Assert.Equal(imageData, packet.FullFrameData);
        Assert.Empty(packet.ChangedTiles);
    }

    /// <summary>
    /// verifies that a changed tile packet stores its tile list
    /// </summary>
    [Fact]
    public void ConstructorChangedTilesPacketStoresTiles()
    {
        Tile tile = new(0, 0, 1, 1, 123, new byte[] { 5, 6 });
        ScreenDataPacket packet = new(
            "stream-B",
            11,
            s_captureTime,
            128,
            128,
            DataPacketType.ChangedTiles,
            changedTiles: new[] { tile }
            );

        Assert.Equal("stream-B", packet.StreamId);
        Assert.Equal(11, packet.FrameNumber);
        Assert.Equal(DataPacketType.ChangedTiles, packet.Type);
        Assert.False(packet.IsFullFrame);
        Assert.False(packet.IsPulse);
        Assert.Null(packet.FullFrameData);
        Assert.Single(packet.ChangedTiles);
        Assert.Same(tile, packet.ChangedTiles[0]);
    }

    /// <summary>
    /// verifies that a pulse packet carries metadata but no image data
    /// </summary>
    [Fact]
    public void ConstructorPulsePacketHasNoPayload()
    {
        ScreenDataPacket packet = new(
            "stream-A",
            12,
            s_captureTime,
            1280,
            720,
            DataPacketType.Pulse);

        Assert.True(packet.IsPulse);
        Assert.False(packet.IsFullFrame);
        Assert.Equal(12, packet.FrameNumber);
        Assert.Null(packet.FullFrameData);
        Assert.Empty(packet.ChangedTiles);
    }

    /// <summary>
    /// verifies that a pulse packet cannot carry a full frame
    /// </summary>
    [Fact]
    public void ConstructorPulseWithImageDataThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                s_captureTime,
                128,
                128,
                DataPacketType.Pulse,
                fullFrameData: new byte[] { 1 }));
    }

    /// <summary>
    /// verifies that a pulse packet cannot carry tiles
    /// </summary>
    [Fact]
    public void ConstructorPulseWithTilesThrows()
    {
        Tile tile = new(0, 0, 1, 1, 123, new byte[] { 5 });

        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                s_captureTime,
                128,
                128,
                DataPacketType.Pulse,
                changedTiles: new[] { tile }));
    }



    /// <summary>
    /// verifies that an empty stream ID is rejected
    /// </summary>
    [Fact]
    public void ConstructorEmptyStreamIdThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "",
                1,
                s_captureTime,
                128,
                128,
                DataPacketType.FullFrame,
                fullFrameData: new byte[] { 1 }));
    }

    /// <summary>
    /// verifies that a negative frame number is rejected.
    /// </summary>
    [Fact]
    public void ConstructorNegativeFrameNumberThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                -1,
                s_captureTime,
                128,
                128,
                DataPacketType.FullFrame,
                fullFrameData: new byte[] { 1 }));
    }

    /// <summary>
    /// verifies that local and unspecified timestamps are rejected
    /// </summary>
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ConstructorNonUtcTimestampThrows(DateTimeKind kind)
    {
        DateTime timestamp = DateTime.SpecifyKind(s_captureTime, kind);

        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                timestamp,
                128,
                128,
                DataPacketType.Pulse));
    }

    /// <summary>
    /// verifies that non-positive frame dimensions are rejected
    /// </summary>
    [Theory]
    [InlineData(0, 128)]
    [InlineData(128, 0)]
    [InlineData(-1, 128)]
    public void ConstructorInvalidResolutionThrows(int width, int height)
    {
        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                s_captureTime,
                width,
                height,
                DataPacketType.Pulse));
    }

    /// <summary>
    /// verifies that a non-positive tile size is rejected
    /// </summary>
    [Fact]
    public void ConstructorInvalidTileSizeThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                s_captureTime,
                128,
                128,
                DataPacketType.Pulse,
                tileSize: 0));
    }



    /// <summary>
    /// verifies that a full-frame packet without image data is rejected
    /// </summary>
    [Fact]
    public void ConstructorFullFrameWithoutDataThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                s_captureTime,
                128,
                128,
                DataPacketType.FullFrame));
    }

    /// <summary>
    /// verifies that a changed-tile packet without tiles is rejected
    /// </summary>
    [Fact]
    public void ConstructorChangedTilesWithoutTilesThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                s_captureTime,
                128,
                128,
                DataPacketType.ChangedTiles));
    }

    /// <summary>
    /// verifies that a packet cannot contain both payload types
    /// </summary>
    [Fact]
    public void ConstructorFullFrameAndTilesTogetherThrows()
    {
        Tile tile = new(0, 0, 1, 1, 123, new byte[] { 5 });

        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                s_captureTime,
                128,
                128,
                DataPacketType.FullFrame,
                fullFrameData: new byte[] { 1 },
                changedTiles: new[] { tile }));
    }

    /// <summary>
    /// verifies that tiles which do not fit the frame's tile grid are rejected
    /// </summary>
    [Theory]
    [InlineData(10, 0, 64, 64)]   // not on the grid
    [InlineData(0, 0, 65, 64)]    // larger than a tile
    [InlineData(128, 0, 64, 64)]  // outside a 128-wide frame
    [InlineData(64, 64, 64, 64)]  // runs past the bottom of a 100-high frame
    public void ConstructorTileNotFittingGridThrows(int x, int y, int width, int height)
    {
        Tile tile = new(x, y, width, height, 123, new byte[] { 5 });

        Assert.Throws<ArgumentException>(() =>
            new ScreenDataPacket(
                "stream-A",
                1,
                s_captureTime,
                128,
                100,
                DataPacketType.ChangedTiles,
                changedTiles: new[] { tile }));
    }

    /// <summary>
    /// verifies that a partial edge tile is accepted
    /// </summary>
    [Fact]
    public void ConstructorPartialEdgeTileIsAccepted()
    {
        Tile tile = new(64, 64, 36, 36, 123, new byte[] { 5 });

        ScreenDataPacket packet = new(
            "stream-A",
            1,
            s_captureTime,
            100,
            100,
            DataPacketType.ChangedTiles,
            changedTiles: new[] { tile });

        Assert.Single(packet.ChangedTiles);
    }

    /// <summary>
    /// verifies that tile pixel positions convert to grid indices
    /// </summary>
    [Fact]
    public void GetTileIndexReturnsColumnAndRow()
    {
        Tile tile = new(128, 64, 64, 64, 123, new byte[] { 5 });

        ScreenDataPacket packet = new(
            "stream-A",
            1,
            s_captureTime,
            256,
            256,
            DataPacketType.ChangedTiles,
            changedTiles: new[] { tile });

        Assert.Equal((2, 1), packet.GetTileIndex(tile));
    }



    /// <summary>
    /// verifies that later changes to the supplied full-frame array
    /// do not change the packet's stored bytes
    /// </summary>
    [Fact]
    public void ConstructorFullFrameDataCopiesInputArray()
    {
        byte[] imageData = { 1, 2, 3 };

        ScreenDataPacket packet = new(
            "stream-A",
            1,
            s_captureTime,
            128,
            128,
            DataPacketType.FullFrame,
            fullFrameData: imageData);

        imageData[0] = 99;

        Assert.Equal(new byte[] { 1, 2, 3 }, packet.FullFrameData);
    }
}
