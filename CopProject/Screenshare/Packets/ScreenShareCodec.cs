// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace ScreenShare;

/// <summary>
/// converts ScreenShare messages to and from bytes for Networking.
///
/// every message starts with a 2-byte header: protocol version, then message kind.
/// all integers are little-endian; strings are a u16 byte length followed by UTF-8.
///
/// data packet:
///   streamId(str) frameNumber(i64) timestamp(i64 UTC ticks) width(i32) height(i32) tileSize(i32) type(u8)
///   FullFrame:    dataLength(i32) data
///   ChangedTiles: count(i32), per tile: x(i32) y(i32) width(i32) height(i32) hash(u64) dataLength(i32) data
///   Pulse:        nothing more
///
/// control messages write their fields in constructor order; a participant is id(str) endpoint(str).
/// </summary>

public static class ScreenShareCodec
{
    /// <summary>
    /// version of protocol written in message header
    /// </summary>
    public const byte ProtocolVersion = 1;

    public const int MaxIdBytes = 128;

    public const int MaxDisplayNameBytes = 256;

    public const int MaxEndpointBytes = 64;

    public const int MaxFrameDimension = 16384;

    /// <summary>
    /// Image data allowed in one packet
    /// </summary>
    public const int MaxImageBytes = 32 * 1024 * 1024;

    /// <summary>
    /// endpoints allowed in a send-to list
    /// </summary>
    public const int MaxSendToEntries = 1024;

    private const int HeaderLength = 2;

    /// <summary>
    /// to weed off invalid UTF-8
    /// </summary>
    private static readonly UTF8Encoding s_strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);


    /// converts a message to bytes
    /// </summary>
    /// <exception cref="ArgumentNullException">the message is null</exception>
    /// <exception cref="ArgumentException">
    /// the message type is unknown, or a field exceeds a limit the receiver would reject
    /// </exception>
    public static byte[] Encode(IScreenShareMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(ProtocolVersion);
        writer.Write((byte)message.Kind);

        // encode by type of message
        switch (message)
        {
            case ScreenDataPacket packet:
                WriteDataPacket(writer, packet);
                break;

            case StreamStarted started:
                WriteString(writer, started.StreamId, MaxIdBytes);
                WriteParticipant(writer, started.Sharer);
                WriteString(writer, started.DisplayName, MaxDisplayNameBytes);
                WriteDimension(writer, started.Width);
                WriteDimension(writer, started.Height);
                break;

            case StreamStopped stopped:
                WriteString(writer, stopped.StreamId, MaxIdBytes);
                break;

            case Subscribe subscribe:
                WriteParticipant(writer, subscribe.Viewer);
                WriteString(writer, subscribe.StreamId, MaxIdBytes);
                WriteDimension(writer, subscribe.MaxHeight);
                break;

            case Unsubscribe unsubscribe:
                WriteParticipant(writer, unsubscribe.Viewer);
                WriteString(writer, unsubscribe.StreamId, MaxIdBytes);
                break;

            case RequestFullFrame request:
                WriteParticipant(writer, request.Viewer);
                WriteString(writer, request.StreamId, MaxIdBytes);
                break;

            case FrameAck ack:
                WriteParticipant(writer, ack.Viewer);
                WriteString(writer, ack.StreamId, MaxIdBytes);
                writer.Write(ack.FrameNumber);
                break;

            case UpdateSendToList update:
                WriteString(writer, update.StreamId, MaxIdBytes);
                if (update.SendTo.Count > MaxSendToEntries)
                {
                    throw new ArgumentException(
                        $"send-to list cannot exceed {MaxSendToEntries} entries", nameof(message));
                }
                writer.Write(update.SendTo.Count);
                foreach (IPEndPoint endpoint in update.SendTo)
                {
                    WriteString(writer, endpoint.ToString(), MaxEndpointBytes);
                }
                break;

            case Hello hello:
                WriteParticipant(writer, hello.Viewer);
                break;

            default:
                throw new ArgumentException(
                    $"unsupported message type {message.GetType().Name}", nameof(message));
        }

        writer.Flush();
        return stream.ToArray();
    }


    public static bool Decode(byte[]? data, [NotNullWhen(true)] out IScreenShareMessage? message)
    {
        message = null;

        if (data == null || data.Length < HeaderLength)
        {
            return false;
        }

        try
        {
            var reader = new Reader(data);

            if (reader.ReadByte() != ProtocolVersion)
            {
                return false;
            }

            var kind = (ScreenShareMessageKind)reader.ReadByte();

            IScreenShareMessage? decoded = kind switch {
                ScreenShareMessageKind.Data => ReadDataPacket(reader),
                ScreenShareMessageKind.StreamStarted => new StreamStarted(
                    reader.ReadString(MaxIdBytes),
                    ReadParticipant(reader),
                    reader.ReadString(MaxDisplayNameBytes),
                    ReadDimension(reader),
                    ReadDimension(reader)),
                ScreenShareMessageKind.StreamStopped => new StreamStopped(
                    reader.ReadString(MaxIdBytes)),
                ScreenShareMessageKind.Subscribe => new Subscribe(
                    ReadParticipant(reader),
                    reader.ReadString(MaxIdBytes),
                    ReadDimension(reader)),
                ScreenShareMessageKind.Unsubscribe => new Unsubscribe(
                    ReadParticipant(reader),
                    reader.ReadString(MaxIdBytes)),
                ScreenShareMessageKind.RequestFullFrame => new RequestFullFrame(
                    ReadParticipant(reader),
                    reader.ReadString(MaxIdBytes)),
                ScreenShareMessageKind.FrameAck => new FrameAck(
                    ReadParticipant(reader),
                    reader.ReadString(MaxIdBytes),
                    reader.ReadInt64()),
                ScreenShareMessageKind.UpdateSendToList => ReadUpdateSendToList(reader),
                ScreenShareMessageKind.Hello => new Hello(ReadParticipant(reader)),
                _ => null
            };

            // trailing bytes mean the sender and receiver disagree about the layout
            if (decoded == null || !reader.IsAtEnd)
            {
                return false;
            }

            message = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }


    private static void WriteDataPacket(BinaryWriter writer, ScreenDataPacket packet)
    {
        WriteString(writer, packet.StreamId, MaxIdBytes);
        writer.Write(packet.FrameNumber);
        writer.Write(packet.Timestamp.Ticks);
        WriteDimension(writer, packet.Width);
        WriteDimension(writer, packet.Height);
        WriteDimension(writer, packet.TileSize);
        writer.Write((byte)packet.Type);

        switch (packet.Type)
        {
            case DataPacketType.FullFrame:
                WriteBytes(writer, packet.FullFrameData!, MaxImageBytes);
                break;

            case DataPacketType.ChangedTiles:
                writer.Write(packet.ChangedTiles.Count);

                long totalBytes = 0;
                foreach (Tile tile in packet.ChangedTiles)
                {
                    byte[] data = tile.Data;
                    totalBytes += data.Length;
                    if (totalBytes > MaxImageBytes)
                    {
                        throw new ArgumentException(
                            $"tile data cannot exceed {MaxImageBytes} bytes in total", nameof(packet));
                    }

                    writer.Write(tile.X);
                    writer.Write(tile.Y);
                    writer.Write(tile.Width);
                    writer.Write(tile.Height);
                    writer.Write(tile.Hash);
                    writer.Write(data.Length);
                    writer.Write(data);
                }
                break;
        }
    }


    private static ScreenDataPacket ReadDataPacket(Reader reader)
    {
        string streamId = reader.ReadString(MaxIdBytes);
        long frameNumber = reader.ReadInt64();
        var timestamp = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
        int width = ReadDimension(reader);
        int height = ReadDimension(reader);
        int tileSize = ReadDimension(reader);
        var type = (DataPacketType)reader.ReadByte();

        switch (type)
        {
            case DataPacketType.FullFrame:
                return new ScreenDataPacket(
                    streamId, frameNumber, timestamp, width, height, type,
                    fullFrameData: reader.ReadBytes(MaxImageBytes),
                    tileSize: tileSize);

            case DataPacketType.ChangedTiles:
                int count = reader.ReadInt32();
                long maxTiles = (long)((width + tileSize - 1) / tileSize) * ((height + tileSize - 1) / tileSize);
                if (count <= 0 || count > maxTiles)
                {
                    throw new FormatException("tile count does not fit the frame");
                }

                var tiles = new List<Tile>(count);
                int remainingBytes = MaxImageBytes;
                for (int i = 0; i < count; i++)
                {
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();
                    int tileWidth = reader.ReadInt32();
                    int tileHeight = reader.ReadInt32();

                    if (x < 0 || y < 0 || tileWidth <= 0 || tileHeight <= 0
                         || (long)x + tileWidth > width || (long)y + tileHeight > height)
                    {
                        throw new FormatException("tile does not fit the frame");
                    }

                    ulong hash = reader.ReadUInt64();
                    byte[] data = reader.ReadBytes(remainingBytes);
                    remainingBytes -= data.Length;

                    tiles.Add(new Tile(x, y, tileWidth, tileHeight, hash, data));
                }

                return new ScreenDataPacket(
                    streamId, frameNumber, timestamp, width, height, type,
                    changedTiles: tiles,
                    tileSize: tileSize);

            case DataPacketType.Pulse:
                return new ScreenDataPacket(
                    streamId, frameNumber, timestamp, width, height, type,
                    tileSize: tileSize);

            default:
                throw new FormatException("unknown data packet type");
        }
    }


    private static UpdateSendToList ReadUpdateSendToList(Reader reader)
    {
        string streamId = reader.ReadString(MaxIdBytes);
        int count = reader.ReadInt32();
        if (count < 0 || count > MaxSendToEntries)
        {
            throw new FormatException("send-to list length is out of range");
        }

        var sendTo = new List<IPEndPoint>(count);
        for (int i = 0; i < count; i++)
        {
            sendTo.Add(ReadEndpoint(reader));
        }

        return new UpdateSendToList(streamId, sendTo);
    }


    private static void WriteParticipant(BinaryWriter writer, Participant participant)
    {
        WriteString(writer, participant.Id, MaxIdBytes);
        WriteString(writer, participant.Endpoint.ToString(), MaxEndpointBytes);
    }


    private static Participant ReadParticipant(Reader reader)
    {
        return new Participant(reader.ReadString(MaxIdBytes), ReadEndpoint(reader));
    }


    private static IPEndPoint ReadEndpoint(Reader reader)
    {
        return IPEndPoint.Parse(reader.ReadString(MaxEndpointBytes));
    }


    private static void WriteDimension(BinaryWriter writer, int value)
    {
        if (value <= 0 || value > MaxFrameDimension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), $"dimensions cannot exceed {MaxFrameDimension} or be less than zero");
        }

        writer.Write(value);
    }


    private static int ReadDimension(Reader reader)
    {
        int value = reader.ReadInt32();
        if (value <= 0 || value > MaxFrameDimension)
        {
            throw new FormatException("dimension is out of range");
        }

        return value;
    }


    private static void WriteString(BinaryWriter writer, string value, int maxBytes)
    {
        byte[] bytes = s_strictUtf8.GetBytes(value);
        if (bytes.Length > maxBytes)
        {
            throw new ArgumentException($"text cannot exceed {maxBytes} UTF-8 bytes", nameof(value));
        }

        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }


    private static void WriteBytes(BinaryWriter writer, byte[] value, int maxBytes)
    {
        if (value.Length > maxBytes)
        {
            throw new ArgumentException($"data cannot exceed {maxBytes} bytes", nameof(value));
        }

        writer.Write(value.Length);
        writer.Write(value);
    }


    /// <summary>
    /// reads fields from received bytes
    /// throws FormatException if there are extraneous bytes
    /// </summary>
    private sealed class Reader
    {
        private readonly byte[] _data;
        private int _position;

        public Reader(byte[] data)
        {
            _data = data;
        }

        public bool IsAtEnd => _position == _data.Length;

        public byte ReadByte()
        {
            return Take(1)[0];
        }


        public int ReadInt32()
        {
            return BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        }

        public long ReadInt64()
        {
            return BinaryPrimitives.ReadInt64LittleEndian(Take(8));
        }

        public ulong ReadUInt64()
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
        }

        public string ReadString(int maxBytes)
        {
            int length = BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
            if (length > maxBytes)
            {
                throw new FormatException("text is longer than allowed");
            }

            return s_strictUtf8.GetString(Take(length));
        }

        public byte[] ReadBytes(int maxBytes)
        {
            int length = ReadInt32();
            if (length < 0 || length > maxBytes)
            {
                throw new FormatException("data length is out of range");
            }

            return Take(length).ToArray();
        }

        private ReadOnlySpan<byte> Take(int count)
        {
            // compare against what is left, so a huge length cannot cause a huge allocation
            if (count > _data.Length - _position)
            {
                throw new FormatException("message is truncated");
            }

            ReadOnlySpan<byte> span = _data.AsSpan(_position, count);
            _position += count;
            return span;
        }
    }
}
