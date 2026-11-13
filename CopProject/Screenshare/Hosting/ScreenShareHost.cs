using System.Drawing;
using System.Net;
using Screenshare;

namespace ScreenShare;
internal sealed partial class ScreenShareHost : IDisposable
{
    private readonly Participant _self;
    private readonly IRealyOutput _output;
    private readonly ScreenShareOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, HostedStream> _streams = new();

    /// <param name="self">the host's own participant, used as the sender of acks and requests</param>
    public ScreenShareHost(Participant self, IRealyOutput output, ScreenShareOptions options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(self);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _self = self;
        _output = output;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <summary>gets the ids of the running streams</summary>
    public IReadOnlyCollection<string> StreamIds => _streams.Keys;

    /// <summary>gets the endpoints subscribed to a stream, or an empty list</summary>
    public IReadOnlyCollection<IPEndPoint> GetViewers(string streamId)
    {
        return _streams.TryGetValue(streamId, out HostedStream? stream)
            ? stream.Viewers.Keys
            : Array.Empty<IPEndPoint>();
    }

    /// <summary>
    /// handles a message that a participant, or the host itself, sent to the host
    /// </summary>
    /// <param name="message">the decoded message</param>
    /// <param name="bytes">the encoded message, forwarded as is</param>
    public void Handle(IScreenShareMessage message, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(bytes);

        switch (message)
        {
            case Hello hello:
                foreach (HostedStream stream in _streams.Values)
                {
                    Send(hello.Viewer.Endpoint, stream.Started);
                }
                break;

            case StreamStarted started:
                OnStreamStarted(started, bytes);
                break;

            case StreamStopped stopped:
                if (RemoveStream(stopped.StreamId))
                {
                    _output.Broadcast(stopped, bytes);
                }
                break;

            case Subscribe subscribe:
                OnSubscribe(subscribe);
                break;

            case Unsubscribe unsubscribe:
                if (_streams.TryGetValue(unsubscribe.StreamId, out HostedStream? unsubscribed))
                {
                    unsubscribed.Viewers.Remove(unsubscribe.Viewer.Endpoint);
                }
                break;

            case ScreenDataPacket packet:
                OnData(packet, bytes);
                break;

            case FrameAck ack:
                OnViewerAck(ack);
                break;

            case RequestFullFrame request:
                OnFullFrameRequest(request);
                break;
        }
    }

    /// <summary>
    /// drops everything belonging to a participant who left: their subscriptions, and their
    /// stream if they were sharing
    /// </summary>
    public void OnParticipantLeft(string participantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(participantId);

        foreach (HostedStream stream in _streams.Values.ToList())
        {
            if (stream.Sharer.Id == participantId)
            {
                RemoveStream(stream.Started.StreamId);
                Broadcast(new StreamStopped(stream.Started.StreamId));
                continue;
            }

            foreach (ViewerState viewer in stream.Viewers.Values.Where(v => v.Viewer.Id == participantId).ToList())
            {
                stream.Viewers.Remove(viewer.Viewer.Endpoint);
            }
        }
    }

    public void Dispose()
    {
        foreach (HostedStream stream in _streams.Values)
        {
            stream.Dispose();
        }

        _streams.Clear();
    }

    private void OnStreamStarted(StreamStarted started, byte[] bytes)
    {
        // a restarted stream numbers its frames from 0 again, so existing viewers start over
        // with a full frame
        List<Participant> carriedViewers = _streams.TryGetValue(started.StreamId, out HostedStream? previous)
            ? previous.Viewers.Values.Select(v => v.Viewer).ToList()
            : new List<Participant>();
        RemoveStream(started.StreamId);

        var stream = new HostedStream(started, _timeProvider);
        stream.Applier.FullFrameRequested += _ =>
            Send(stream.Sharer.Endpoint, new RequestFullFrame(_self, started.StreamId));

        foreach (Participant viewer in carriedViewers)
        {
            stream.Viewers[viewer.Endpoint] = new ViewerState(viewer);
        }

        _streams[started.StreamId] = stream;
        _output.Broadcast(started, bytes);
    }

    private void OnSubscribe(Subscribe subscribe)
    {
        if (!_streams.TryGetValue(subscribe.StreamId, out HostedStream? stream))
        {
            return;
        }

        // a (re)subscribing viewer starts with a fresh stitcher, so treat it as new
        var viewer = new ViewerState(subscribe.Viewer);
        stream.Viewers[subscribe.Viewer.Endpoint] = viewer;

        TrySendFullFrameFromCache(stream, viewer);
    }

    private bool RemoveStream(string streamId)
    {
        if (!_streams.Remove(streamId, out HostedStream? stream))
        {
            return false;
        }

        stream.Dispose();
        return true;
    }

    private void Send(IPEndPoint to, IScreenShareMessage message)
    {
        _output.Send(to, message, ScreenShareCodec.Encode(message));
    }

    private void Broadcast(IScreenShareMessage message)
    {
        _output.Broadcast(message, ScreenShareCodec.Encode(message));
    }
}
