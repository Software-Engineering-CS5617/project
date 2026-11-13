using System.Drawing;
using System.Net;

namespace ScreenShare;

// frames, acks, flow control and the host's copy of each stream.
// the other half, streams and viewers, is in ScreenShareHost.cs.
internal sealed partial class ScreenShareHost
{
    /// <summary>
    /// drops viewers that have left frames unacknowledged for longer than the viewer timeout,
    /// e.g. a client that crashed without leaving the meeting
    /// </summary>
    public void RemoveTimedOutViewers()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (HostedStream stream in _streams.Values)
        {
            List<ViewerState> timedOut = stream.Viewers.Values
                .Where(v => v.HasUnacknowledgedFrames && now - v.WaitingSince >= _options.ViewerTimeout)
                .ToList();

            foreach (ViewerState viewer in timedOut)
            {
                stream.Viewers.Remove(viewer.Viewer.Endpoint);
            }
        }
    }

    private void OnData(ScreenDataPacket packet, byte[] bytes)
    {
        if (!_streams.TryGetValue(packet.StreamId, out HostedStream? stream))
        {
            return;
        }

        stream.Applier.Apply(packet);

        // acknowledge receipt, so the sharer can send its next frame
        Send(stream.Sharer.Endpoint, new FrameAck(_self, packet.StreamId, packet.FrameNumber));

        foreach (ViewerState viewer in stream.Viewers.Values)
        {
            if (viewer.NeedsFullFrame)
            {
                if (packet.IsFullFrame && !IsLagging(viewer))
                {
                    SendToViewer(viewer, packet, bytes);
                    viewer.NeedsFullFrame = false;
                }
                else
                {
                    TrySendFullFrameFromCache(stream, viewer);
                }

                continue;
            }

            if (IsLagging(viewer))
            {
                // stop sending deltas; once it catches up it gets one full frame instead
                viewer.NeedsFullFrame = true;
                continue;
            }

            SendToViewer(viewer, packet, bytes);
        }
    }

    private void OnViewerAck(FrameAck ack)
    {
        if (!_streams.TryGetValue(ack.StreamId, out HostedStream? stream) ||
            !stream.Viewers.TryGetValue(ack.Viewer.Endpoint, out ViewerState? viewer))
        {
            return;
        }

        if (ack.FrameNumber > viewer.LastAcked && ack.FrameNumber <= viewer.LastSent)
        {
            viewer.LastAcked = ack.FrameNumber;
            viewer.WaitingSince = _timeProvider.GetUtcNow();
        }
    }

    private void OnFullFrameRequest(RequestFullFrame request)
    {
        if (!_streams.TryGetValue(request.StreamId, out HostedStream? stream) ||
            !stream.Viewers.TryGetValue(request.Viewer.Endpoint, out ViewerState? viewer))
        {
            return;
        }

        // if the cache cannot serve it now, the next full frame or cached frame will
        viewer.NeedsFullFrame = true;
        TrySendFullFrameFromCache(stream, viewer);
    }

    /// <summary>
    /// sends the host's latest picture of the stream as a full frame, if the host has one
    /// and the viewer is not too far behind
    /// </summary>
    private void TrySendFullFrameFromCache(HostedStream stream, ViewerState viewer)
    {
        if (!viewer.NeedsFullFrame || IsLagging(viewer) || stream.Stitcher.NeedsFullFrame)
        {
            return;
        }

        (ScreenDataPacket packet, byte[] bytes) = stream.GetCachedFullFrame(_timeProvider.GetUtcNow().UtcDateTime);

        SendToViewer(viewer, packet, bytes);
        viewer.NeedsFullFrame = false;
    }

    private void SendToViewer(ViewerState viewer, ScreenDataPacket packet, byte[] bytes)
    {
        // the timeout clock starts when the viewer goes from fully caught up to owing an ack
        if (!viewer.HasUnacknowledgedFrames)
        {
            viewer.WaitingSince = _timeProvider.GetUtcNow();
        }

        _output.Send(viewer.Viewer.Endpoint, packet, bytes);
        viewer.LastSent = packet.FrameNumber;
    }

    private bool IsLagging(ViewerState viewer)
    {
        return viewer.LastSent - viewer.LastAcked > _options.MaxViewerLag;
    }

    /// <summary>
    /// a running stream: who shares it, who watches it, and the host's copy of its picture
    /// </summary>
    private sealed class HostedStream : IDisposable
    {
        private (long FrameNumber, ScreenDataPacket Packet, byte[] Bytes)? _cachedFullFrame;

        public StreamStarted Started { get; }

        public Participant Sharer => Started.Sharer;

        public ImageStitcher Stitcher { get; } = new();

        public ScreenPacketApplier Applier { get; }

        public Dictionary<IPEndPoint, ViewerState> Viewers { get; } = new();

        public HostedStream(StreamStarted started, TimeProvider timeProvider)
        {
            Started = started;
            Applier = new ScreenPacketApplier(started.StreamId, Stitcher, timeProvider);
        }

        /// <summary>
        /// encodes the current picture as a full-frame packet, once per frame number,
        /// so several viewers joining at once share one PNG encode
        /// </summary>
        public (ScreenDataPacket Packet, byte[] Bytes) GetCachedFullFrame(DateTime nowUtc)
        {
            long frameNumber = Stitcher.LastFrameNumber;

            if (_cachedFullFrame is { } cached && cached.FrameNumber == frameNumber)
            {
                return (cached.Packet, cached.Bytes);
            }

            using Bitmap frame = Stitcher.GetCurrentFrame()!;

            var packet = new ScreenDataPacket(
                Started.StreamId,
                frameNumber,
                nowUtc,
                frame.Width,
                frame.Height,
                DataPacketType.FullFrame,
                fullFrameData: PngEncoder.Encode(frame));
            byte[] bytes = ScreenShareCodec.Encode(packet);

            _cachedFullFrame = (frameNumber, packet, bytes);
            return (packet, bytes);
        }

        public void Dispose()
        {
            Stitcher.Dispose();
        }
    }

    /// <summary>
    /// how far a viewer has got with a stream
    /// </summary>
    private sealed class ViewerState
    {
        public ViewerState(Participant viewer)
        {
            Viewer = viewer;
        }

        public Participant Viewer { get; }

        /// <summary>true until the viewer has been sent a full frame to start from</summary>
        public bool NeedsFullFrame { get; set; } = true;

        public long LastSent { get; set; } = -1;

        public long LastAcked { get; set; } = -1;

        /// <summary>when the viewer last caught up, or started owing an ack</summary>
        public DateTimeOffset WaitingSince { get; set; }

        public bool HasUnacknowledgedFrames => LastSent > LastAcked;
    }
}
