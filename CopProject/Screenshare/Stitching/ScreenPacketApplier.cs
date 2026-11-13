using System.Drawing;

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
            RequestFullFrameIfDue();
            return PacketApplyOutcome.NeedsFullFrame;
        }

        return PacketApplyOutcome.Ignored;
    }

    private bool ApplyFullFrame(ScreenDataPacket packet)
    {
        using Bitmap? frame = TryDecodeImage(packet.FullFrameData!);

        if (frame == null || frame.Width != packet.Width || frame.Height != packet.Height)
        {
            return false;
        }

        return _stitcher.ApplyFullFrame(packet.FrameNumber, frame);
    }

    private bool ApplyChangedTiles(ScreenDataPacket packet)
    {
        try
        {
            return _stitcher.ApplyChangedTiles(packet.FrameNumber, packet.ChangedTiles);
        }
        catch (ArgumentException)
        {
            // a tile did not decode or did not fit the frame; the stitcher left the frame unchanged
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
        catch (ArgumentException)
        {
            // not a valid image
            return null;
        }
    }
}
