using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ScreenShare;

/// <summary>
/// reads just enough of an encoded message to classify it, without decoding it.
/// data packets carry whole images, and the traffic observers see every one of them,
/// so they must not pay for a full ScreenShareCodec.Decode.
///
/// relies on the layout documented in ScreenShareCodec: version(u8) kind(u8), then for data
/// packets and StreamStopped a streamId as u16 little-endian length plus UTF-8.
/// </summary>
public static class WireFormat
{
    /// <returns>false if the bytes are not a message of this protocol version</returns>
    public static bool TryPeekKind(byte[]? bytes, out ScreenShareMessageKind kind)
    {
        kind = default;

        if (bytes == null || bytes.Length < 2 || bytes[0] != ScreenShareCodec.ProtocolVersion)
        {
            return false;
        }

        kind = (ScreenShareMessageKind)bytes[1];
        return true;
    }

    /// <returns>false unless the message is a data packet or StreamStopped with a readable stream id</returns>
    public static bool TryPeekStreamId(byte[]? bytes, [NotNullWhen(true)] out string? streamId)
    {
        streamId = null;

        if (!TryPeekKind(bytes, out ScreenShareMessageKind kind) ||
            (kind != ScreenShareMessageKind.Data && kind != ScreenShareMessageKind.StreamStopped) ||
            bytes!.Length < 4)
        {
            return false;
        }

        int length = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2, 2));
        if (length == 0 || length > ScreenShareCodec.MaxIdBytes || bytes.Length < 4 + length)
        {
            return false;
        }

        streamId = Encoding.UTF8.GetString(bytes, 4, length);
        return true;
    }
}
