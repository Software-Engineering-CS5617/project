using System.Buffers.Binary;
using System.Net;
using System.Text;
using Xunit;

namespace ScreenShare.Tests;

/// <summary>
/// tests that every message survives encoding, and that malformed bytes are rejected without throwing
/// </summary>
public class ScreenShareCodecTests
{
    private static readonly DateTime s_captureTime = new(2026, 11, 3, 10, 30, 0, DateTimeKind.Utc);
    private static readonly Participant s_alice = new("alice", IPEndPoint.Parse("192.168.1.10:50123"));

    // byte offsets in a data packet whose stream id is the single character "s"
    private const int WidthOffset = 21;
    private const int TileSizeOffset = 29;
    private const int TypeOffset = 33;
    private const int CountOrLengthOffset = 34;
    private const int FirstTileDataLengthOffset = 62;

    public static TheoryData<IScreenShareMessage> ControlMessages => new()
    {
        new StreamStarted("alice-screen", s_alice, "Alice", 1920, 1080),
        new StreamStopped("alice-screen"),
        new Subscribe(s_alice, "bob-screen", 720),
        new Unsubscribe(s_alice, "bob-screen"),
        new RequestFullFrame(s_alice, "bob-screen"),
        new FrameAck(s_alice, "bob-screen", 42),
        new StreamStarted("ipv6-screen", new Participant("v6", IPEndPoint.Parse("[::1]:5000")), "IPv6 user", 1280, 720),
    };

    [Theory]
    [MemberData(nameof(ControlMessages))]
    public void ControlMessage_RoundTrips(IScreenShareMessage message)
    {
        byte[] bytes = ScreenShareCodec.Encode(message);

        Assert.True(ScreenShareCodec.Decode(bytes, out IScreenShareMessage? decoded));
        Assert.Equal(message, decoded);
    }

    [Fact]
    public void UpdateSendToList_RoundTrips()
    {
        var message = new UpdateSendToList(
            "alice-screen",
            new[] { IPEndPoint.Parse("10.0.0.2:4000"), IPEndPoint.Parse("[fe80::1]:4001") });

        Assert.True(ScreenShareCodec.Decode(ScreenShareCodec.Encode(message), out IScreenShareMessage? decoded));

        UpdateSendToList update = Assert.IsType<UpdateSendToList>(decoded);
        Assert.Equal("alice-screen", update.StreamId);
        Assert.Equal(message.SendTo, update.SendTo);
    }

    [Fact]
    public void UpdateSendToList_EmptyListRoundTrips()
    {
        var message = new UpdateSendToList("alice-screen", Array.Empty<IPEndPoint>());

        Assert.True(ScreenShareCodec.Decode(ScreenShareCodec.Encode(message), out IScreenShareMessage? decoded));
        Assert.Empty(Assert.IsType<UpdateSendToList>(decoded).SendTo);
    }

    [Fact]
    public void FullFramePacket_RoundTrips()
    {
        var packet = new ScreenDataPacket(
            "alice-screen", 7, s_captureTime, 1280, 720, DataPacketType.FullFrame,
            fullFrameData: new byte[] { 1, 2, 3, 4, 5 });

        ScreenDataPacket decoded = RoundTrip(packet);

        AssertSameHeader(packet, decoded);
        Assert.Equal(packet.FullFrameData, decoded.FullFrameData);
        Assert.Empty(decoded.ChangedTiles);
    }

    [Fact]
    public void ChangedTilesPacket_RoundTrips()
    {
        Tile[] tiles = new[]
        {
            new Tile(0, 0, 64, 64, 0xDEADBEEFCAFEF00D, new byte[] { 9, 8, 7 }),
            new Tile(64, 64, 36, 36, 1, new byte[] { 6 }),
        };
        var packet = new ScreenDataPacket(
            "alice-screen", 8, s_captureTime, 100, 100, DataPacketType.ChangedTiles,
            changedTiles: tiles);

        ScreenDataPacket decoded = RoundTrip(packet);

        AssertSameHeader(packet, decoded);
        Assert.Null(decoded.FullFrameData);
        Assert.Equal(tiles.Length, decoded.ChangedTiles.Count);
        for (int i = 0; i < tiles.Length; i++)
        {
            Assert.Equal(tiles[i].X, decoded.ChangedTiles[i].X);
            Assert.Equal(tiles[i].Y, decoded.ChangedTiles[i].Y);
            Assert.Equal(tiles[i].Width, decoded.ChangedTiles[i].Width);
            Assert.Equal(tiles[i].Height, decoded.ChangedTiles[i].Height);
            Assert.Equal(tiles[i].Hash, decoded.ChangedTiles[i].Hash);
            Assert.Equal(tiles[i].Data, decoded.ChangedTiles[i].Data);
        }
    }

    [Fact]
    public void PulsePacket_RoundTrips()
    {
        var packet = new ScreenDataPacket(
            "alice-screen", 9, s_captureTime, 1280, 720, DataPacketType.Pulse, tileSize: 32);

        ScreenDataPacket decoded = RoundTrip(packet);

        AssertSameHeader(packet, decoded);
        Assert.True(decoded.IsPulse);
    }

    [Fact]
    public void Header_HasVersionAndKind()
    {
        byte[] bytes = ScreenShareCodec.Encode(new StreamStopped("s"));

        Assert.Equal(ScreenShareCodec.ProtocolVersion, bytes[0]);
        Assert.Equal((byte)ScreenShareMessageKind.StreamStopped, bytes[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 1 })]
    public void Decode_RejectsMissingHeader(byte[]? data)
    {
        Assert.False(ScreenShareCodec.Decode(data, out IScreenShareMessage? message));
        Assert.Null(message);
    }

    [Fact]
    public void Decode_RejectsWrongVersion()
    {
        byte[] bytes = ScreenShareCodec.Encode(new StreamStopped("s"));
        bytes[0] = ScreenShareCodec.ProtocolVersion + 1;

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Decode_RejectsUnknownKind(byte kind)
    {
        byte[] bytes = ScreenShareCodec.Encode(new StreamStopped("s"));
        bytes[1] = kind;

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_RejectsTrailingBytes()
    {
        byte[] bytes = ScreenShareCodec.Encode(new StreamStopped("s"));

        Assert.False(ScreenShareCodec.Decode(bytes.Append((byte)0).ToArray(), out _));
    }

    [Fact]
    public void Decode_RejectsEveryTruncation()
    {
        IScreenShareMessage[] messages =
        {
            TilesPacket(),
            new FrameAck(s_alice, "bob-screen", 42),
            new UpdateSendToList("s", new[] { IPEndPoint.Parse("10.0.0.2:4000") }),
        };

        foreach (IScreenShareMessage message in messages)
        {
            byte[] bytes = ScreenShareCodec.Encode(message);

            for (int length = 0; length < bytes.Length; length++)
            {
                Assert.False(
                    ScreenShareCodec.Decode(bytes[..length], out _),
                    $"{message.Kind} truncated to {length} of {bytes.Length} bytes was accepted");
            }
        }
    }

    [Fact]
    public void Decode_RejectsUnknownDataPacketType()
    {
        byte[] bytes = ScreenShareCodec.Encode(PulsePacket());
        bytes[TypeOffset] = 99;

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ScreenShareCodec.MaxFrameDimension + 1)]
    public void Decode_RejectsOutOfRangeWidth(int width)
    {
        byte[] bytes = ScreenShareCodec.Encode(PulsePacket());
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(WidthOffset), width);

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_RejectsZeroTileSize()
    {
        byte[] bytes = ScreenShareCodec.Encode(PulsePacket());
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(TileSizeOffset), 0);

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Decode_RejectsBadFullFrameLength(int length)
    {
        byte[] bytes = ScreenShareCodec.Encode(new ScreenDataPacket(
            "s", 1, s_captureTime, 128, 128, DataPacketType.FullFrame, fullFrameData: new byte[] { 1, 2 }));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(CountOrLengthOffset), length);

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    // a 128x128 frame has only 4 tiles, so 5 is too many
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(int.MaxValue)]
    public void Decode_RejectsBadTileCount(int count)
    {
        byte[] bytes = ScreenShareCodec.Encode(TilesPacket());
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(CountOrLengthOffset), count);

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Decode_RejectsBadTileDataLength(int length)
    {
        byte[] bytes = ScreenShareCodec.Encode(TilesPacket());
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(FirstTileDataLengthOffset), length);

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_RejectsTileOutsideFrame()
    {
        byte[] bytes = ScreenShareCodec.Encode(TilesPacket());

        // move the first tile's x to 128, past the right edge of the 128-wide frame
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(CountOrLengthOffset + 4), 128);

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_RejectsInvalidUtf8()
    {
        byte[] bytes = ScreenShareCodec.Encode(new StreamStopped("s"));

        // 0xFF never appears in valid UTF-8
        bytes[4] = 0xFF;

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_RejectsEmptyStreamId()
    {
        byte[] bytes = { ScreenShareCodec.ProtocolVersion, (byte)ScreenShareMessageKind.StreamStopped, 0, 0 };

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_RejectsOverlongStreamId()
    {
        byte[] id = Encoding.UTF8.GetBytes(new string('a', ScreenShareCodec.MaxIdBytes + 1));
        byte[] bytes = new byte[4 + id.Length];
        bytes[0] = ScreenShareCodec.ProtocolVersion;
        bytes[1] = (byte)ScreenShareMessageKind.StreamStopped;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2), (ushort)id.Length);
        id.CopyTo(bytes, 4);

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_RejectsInvalidEndpoint()
    {
        var message = new Unsubscribe(new Participant("a", IPEndPoint.Parse("1.2.3.4:5")), "s");
        byte[] bytes = ScreenShareCodec.Encode(message);

        // layout: header(2) id-length(2) "a" endpoint-length(2) "1.2.3.4:5"; corrupt the first digit
        bytes[7] = (byte)'x';

        Assert.False(ScreenShareCodec.Decode(bytes, out _));
    }

    // the message is built inside the lambda, so a constructor that validates
    // length and throws ArgumentException still counts as the expected failure
    [Fact]
    public void Encode_RejectsOverlongStreamId()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ScreenShareCodec.Encode(new StreamStopped(new string('a', ScreenShareCodec.MaxIdBytes + 1))));
    }

    // ArgumentOutOfRangeException derives from ArgumentException, so ThrowsAny covers
    // both the constructor and the codec rejecting the dimension
    [Fact]
    public void Encode_RejectsOversizedFrame()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ScreenShareCodec.Encode(new ScreenDataPacket(
                "s", 1, s_captureTime, ScreenShareCodec.MaxFrameDimension + 1, 128, DataPacketType.Pulse)));
    }

    [Fact]
    public void Encode_RejectsUnknownMessageType()
    {
        Assert.Throws<ArgumentException>(() => ScreenShareCodec.Encode(new UnknownMessage()));
    }

    private static ScreenDataPacket PulsePacket()
    {
        return new("s", 1, s_captureTime, 128, 128, DataPacketType.Pulse);
    }

    private static ScreenDataPacket TilesPacket()
    {
        return new("s", 1, s_captureTime, 128, 128, DataPacketType.ChangedTiles,
            changedTiles: new[]
            {
                new Tile(0, 0, 64, 64, 5, new byte[] { 1, 2, 3 }),
                new Tile(64, 64, 64, 64, 6, new byte[] { 4 }),
            });
    }

    private static ScreenDataPacket RoundTrip(ScreenDataPacket packet)
    {
        Assert.True(ScreenShareCodec.Decode(ScreenShareCodec.Encode(packet), out IScreenShareMessage? decoded));
        return Assert.IsType<ScreenDataPacket>(decoded);
    }

    private static void AssertSameHeader(ScreenDataPacket expected, ScreenDataPacket actual)
    {
        Assert.Equal(expected.StreamId, actual.StreamId);
        Assert.Equal(expected.FrameNumber, actual.FrameNumber);
        Assert.Equal(expected.Timestamp, actual.Timestamp);
        Assert.Equal(DateTimeKind.Utc, actual.Timestamp.Kind);
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        Assert.Equal(expected.TileSize, actual.TileSize);
        Assert.Equal(expected.Type, actual.Type);
    }

    private sealed class UnknownMessage : IScreenShareMessage
    {
        public ScreenShareMessageKind Kind => ScreenShareMessageKind.StreamStopped;
    }
}
