using System.Diagnostics;
using System.Drawing;
using ABI.System;

namespace ScreenShare;

/// <summary>
/// applies received data packets of one stream to its stitcher, and asks for a full frame
/// when the stream cannot continue without one (joined mid-stream, or a frame was lost)
/// </summary>
public sealed class ScreenPacketApplier
{
    /// <summary>
    /// shortest time between two full-frame requests, so one lost packet does not cause
    /// a request for every delta that follows it
    /// </summary>
    public static readonly TimeSpan FullFrameRequestInterval = TimeSpan.FromSeconds(1);

    private readonly IImageStitcher _stitcher;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset? _lastFullFrameRequest;

    /// <summary>gets the stream this applier handles</summary>
    public string StreamId { get; }

    /// <summary>
    /// raised with the stream id when a full frame should be requested from the sharer.
    /// raised on the thread that calls Apply.
    /// </summary>
    public event Action<string>? FullFrameRequested;

    /// <param name="streamId">the stream this applier handles</param>
    /// <param name="stitcher">the stitcher holding the stream's frame; it stays owned by the caller</param>
    /// <param name="timeProvider">clock for rate-limiting requests; defaults to the system clock</param>
    /// <exception cref="ArgumentException">the stream id is empty</exception>
    /// <exception cref="ArgumentNullException">the stitcher is null</exception>
    public ScreenPacketApplier(string streamId, IImageStitcher stitcher, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(stitcher);

        StreamId = streamId;
        _stitcher = stitcher;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// applies one received packet. corrupt image data is dropped rather than thrown,
    /// since it comes from the network; the stitcher then sees the next packet as a gap.
    /// </summary>
    /// <exception cref="ArgumentNullException">the packet is null</exception>
    /// <exception cref="ArgumentException">the packet belongs to another stream</exception>
    public PacketApplyOutcome Apply(ScreenDataPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.StreamId != StreamId)
        {
            Trace.TraceWarning(
            "[Screenshare] Packet belongs to an unexpected stream. Expected: {0}, Received: {1}.",
            StreamId,
            packet.StreamId);
            throw new ArgumentException(
                $"packet belongs to stream '{packet.StreamId}', not '{StreamId}'", nameof(packet));
        }

        bool applied = packet.Type switch {
            DataPacketType.FullFrame => ApplyFullFrame(packet),
            DataPacketType.ChangedTiles => ApplyChangedTiles(packet),
            _ => _stitcher.ApplyPulse(packet.FrameNumber)
        };

        if (applied)
        {
            return PacketApplyOutcome.Applied;
        }

        if (_stitcher.NeedsFullFrame)
        {
            Trace.TraceWarning(
            "[Screenshare] Packet could not be applied. Stream: {0}, Frame: {1}. Full frame required.",
            StreamId,
            packet.FrameNumber);
            RequestFullFrameIfDue();
            return PacketApplyOutcome.NeedsFullFrame;
        }

        Trace.TraceWarning(
        "[Screenshare] Packet ignored. Stream: {0}, Frame: {1}, Type: {2}.",
        StreamId,
        packet.FrameNumber,
        packet.Type);
        return PacketApplyOutcome.Ignored;
    }

    private bool ApplyFullFrame(ScreenDataPacket packet)
    {
        byte[] data = packet.FullFrameData!;
        using Bitmap? frame = TryDecodeImage(data);

        if (frame == null || frame.Width != packet.Width || frame.Height != packet.Height)
        {
            Trace.TraceWarning(
            "[Screenshare] Full-frame packet rejected. Stream: {0}, Frame: {1}, Payload size: {2} bytes.",
            StreamId,
            packet.FrameNumber,
            data.Length);
            return false;
        }

        Trace.TraceInformation(
        "[Screenshare] Full-frame packet decoded. Stream: {0}, Frame: {1}, Payload size: {2} bytes.",
        StreamId,
        packet.FrameNumber,
        data.Length);
        return _stitcher.ApplyFullFrame(packet.FrameNumber, frame);
    }

    private bool ApplyChangedTiles(ScreenDataPacket packet)
    {
        try
        {
            long totalPayloadSize = packet.ChangedTiles.Sum(tile => (long)tile.Data.Length);

            Trace.TraceInformation(
                "[Screenshare] Processing delta packet. Stream: {0}, Frame: {1}, Tiles: {2}, Encoded payload size: {3} bytes.",
                StreamId,
                packet.FrameNumber,
                packet.ChangedTiles.Count,
                totalPayloadSize);
            return _stitcher.ApplyChangedTiles(packet.FrameNumber, packet.ChangedTiles);
        }
        catch (ArgumentException exception)
        {
            // a tile did not decode or did not fit the frame; the stitcher left the frame unchanged

            Trace.TraceWarning(
            "[Screenshare] Delta packet rejected. Stream: {0}, Frame: {1}. Reason: {2}",
            StreamId,
            packet.FrameNumber,
            exception.Message);
            return false;
        }
    }

    private void RequestFullFrameIfDue()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (_lastFullFrameRequest is DateTimeOffset last && now - last < FullFrameRequestInterval)
        {
            return;
        }

        _lastFullFrameRequest = now;
        Trace.TraceInformation(
        "[Screenshare] Requesting full frame. Stream: {0}.",
        StreamId);
        FullFrameRequested?.Invoke(StreamId);
    }

    private static Bitmap? TryDecodeImage(byte[] data)
    {
        try
        {
            using MemoryStream stream = new(data);
            using Bitmap decoded = new(stream);

            // Bitmap keeps reading from its stream, so copy it into a standalone bitmap
            return new Bitmap(decoded);
        }
        catch (ArgumentException exception)
        {
            // not a valid image
            Trace.TraceWarning(
            "[Screenshare] Failed to decode image payload. Payload size: {0} bytes. Reason: {1}",
            data.Length,
            exception.Message);
            return null;
        }
    }
}
