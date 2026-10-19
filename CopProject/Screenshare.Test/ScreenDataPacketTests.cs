using ScreenShare;
using Xunit;

namespace Screenshare.Tests;

/// <summary>
/// tests validation and data storage in ScreenDataPacket
/// </summary>

public class ScreenDataPacketTests
{
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
            DataPacketType.FullFrame,
            fullFrameData: imageData
            );

        Assert.Equal("stream-A", packet.StreamId);
        Assert.Equal(10, packet.FrameNumber);
        Assert.Equal(DataPacketType.FullFrame, packet.Type);
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
            DataPacketType.ChangedTiles,
            changedTiles: new[] { tile }
            );

        Assert.Equal("stream-B", packet.StreamId);
        Assert.Equal(11, packet.FrameNumber);
        Assert.Equal(DataPacketType.ChangedTiles, packet.Type);
        Assert.Null(packet.FullFrameData);
        Assert.Single(packet.ChangedTiles);
        Assert.Same(tile, packet.ChangedTiles[0]);
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
                DataPacketType.FullFrame,
                fullFrameData: new byte[] { 1 }));
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
                DataPacketType.FullFrame,
                fullFrameData: new byte[] { 1 },
                changedTiles: new[] { tile }));
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
            DataPacketType.FullFrame,
            fullFrameData: imageData);

        imageData[0] = 99;

        Assert.Equal(new byte[] { 1, 2, 3 }, packet.FullFrameData);
    }
}
