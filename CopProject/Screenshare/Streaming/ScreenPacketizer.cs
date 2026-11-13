using System.Drawing;

namespace ScreenShare;

/// <summary>
/// turns the sender's diff results into numbered data packets for a stream:
/// a full frame becomes a PNG, a delta keeps its tiles, and an unchanged frame becomes a pulse
/// </summary>
public sealed class ScreenPacketizer
{
    private int _width;
    private int _height;

    /// <summary>
    /// gets the stream these packets belong to
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// gets the frame number the next packet will carry. pulses take a number too,
    /// so the receiver can tell a quiet screen from a lost packet.
    /// </summary>
    public long NextFrameNumber { get; private set; }

    /// <exception cref="ArgumentException">the stream id is empty</exception>
    public ScreenPacketizer(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        StreamId = streamId;
    }

    /// <summary>
    /// creates the packet for one diff result and advances the frame number
    /// </summary>
    /// <param name="result">the diff result; it stays owned by the caller</param>
    /// <param name="capturedAtUtc">when the frame was captured, in UTC</param>
    /// <exception cref="ArgumentNullException">the result is null</exception>
    /// <exception cref="ArgumentException">the timestamp is not UTC</exception>
    /// <exception cref="InvalidOperationException">a delta or pulse arrives before any full frame</exception>
    public ScreenDataPacket CreatePacket(ImageDiffResult result, DateTime capturedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);

        ScreenDataPacket packet;

        if (result.IsFullFrame)
        {
            Bitmap frame = result.FullFrame!;

            packet = new ScreenDataPacket(
                StreamId,
                NextFrameNumber,
                capturedAtUtc,
                frame.Width,
                frame.Height,
                DataPacketType.FullFrame,
                fullFrameData: PngEncoder.Encode(frame));

            // deltas and pulses carry no image, so it needs to remember the size from the last full frame.
            // the differ sends a full frame whenever the size changes.
            _width = frame.Width;
            _height = frame.Height;
        }
        else
        {
            if (_width == 0)
            {
                throw new InvalidOperationException(
                    "a full frame must be packetized before deltas or pulses");
            }

            packet = result.IsUnchanged
                ? new ScreenDataPacket(
                    StreamId, NextFrameNumber, capturedAtUtc, _width, _height,
                    DataPacketType.Pulse)
                : new ScreenDataPacket(
                    StreamId, NextFrameNumber, capturedAtUtc, _width, _height,
                    DataPacketType.ChangedTiles,
                    changedTiles: result.ChangedTiles);
        }

        NextFrameNumber++;
        return packet;
    }
}
