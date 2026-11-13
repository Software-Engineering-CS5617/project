using System.Net;

namespace ScreenShare;

/// <summary>a peer the host stopped hearing from. The id is null if it never introduced itself</summary>
public readonly record struct LostPeer(string? ParticipantId, IPEndPoint Endpoint);

/// <summary>
/// host-side: checks which peers have silently vanished (wifi off etc)
/// peer: a sharer is expected to keep sending frames so a long silence means it is gone
///       a viewer is expected to acknowledge frames so no ack implies its gone
/// </summary>
public sealed class PeerLivenessTracker
{
    private readonly ResilienceOptions _options;
    private readonly TimeProvider _time;
    private readonly object _lock = new();
    private readonly Dictionary<IPEndPoint, PeerState> _peers = new();

    /// <summary>
    /// maps stream id to sharer's endpoint. We need it to find out who sent it 
    /// seince a data packet only reveals its stream 
    /// </summary>
    private readonly Dictionary<string, IPEndPoint> _sharerByStream = new();

    public PeerLivenessTracker(ResilienceOptions options, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(time);

        _options = options;
        _time = time;
    }

    /// <summary>records a control message a peer sent to the host</summary>
    public void OnInbound(IScreenShareMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_lock)
        {
            switch (message)
            {
                case StreamStarted started:
                    PeerState sharer = Heard(started.Sharer);
                    sharer.SharedStreamId = started.StreamId;
                    _sharerByStream[started.StreamId] = started.Sharer.Endpoint;
                    break;

                case StreamStopped stopped:
                    ForgetStream(stopped.StreamId);
                    break;

                case Hello hello:
                    Heard(hello.Viewer);
                    break;

                case Subscribe subscribe:
                    Heard(subscribe.Viewer);
                    break;

                case Unsubscribe unsubscribe:
                    Heard(unsubscribe.Viewer);
                    break;

                case RequestFullFrame request:
                    Heard(request.Viewer);
                    break;

                case FrameAck ack:
                    Heard(ack.Viewer);
                    break;
            }
        }
    }

    /// <summary>
    /// records a data packet from a sharer. data packets only reveal their stream
    /// so sender is found with StreamStarted 
    /// </summary>
    public void OnInboundData(string streamId)
    {
        lock (_lock)
        {
            if (_sharerByStream.TryGetValue(streamId, out IPEndPoint? endpoint) &&
                _peers.TryGetValue(endpoint, out PeerState? peer))
            {
                Touch(peer);
            }
        }
    }

    /// <summary>records that the host is sending frames to a peer, so it now owes acknowledgements</summary>
    public void OnOutboundData(IPEndPoint to)
    {
        lock (_lock)
        {
            PeerState peer = GetOrAdd(to);
            if (!peer.Lost && peer.OwedSince == null)
            {
                peer.OwedSince = _time.GetUtcNow();
            }
        }
    }

    public bool IsLost(IPEndPoint endpoint)
    {
        lock (_lock)
        {
            return _peers.TryGetValue(endpoint, out PeerState? peer) && peer.Lost;
        }
    }


    /// <summary>
    /// returns the peers that have just been declared gone
    /// </summary>
    public IReadOnlyList<LostPeer> Sweep()
    {
        lock (_lock)
        {
            var lost = new List<LostPeer>();

            DateTimeOffset now = _time.GetUtcNow();

            foreach ((IPEndPoint endpoint, PeerState peer) in _peers)
            {
                if (peer.Lost)
                {
                    continue;
                }

                bool sharerSilent = peer.SharedStreamId != null
                    && now - peer.LastHeard > _options.PeerSilenceTimeout;
                bool viewerSilent = peer.OwedSince is DateTimeOffset since
                    && now - since > _options.PeerSilenceTimeout;

                if (sharerSilent || viewerSilent)
                {
                    MarkLost(peer);
                    lost.Add(new LostPeer(peer.ParticipantId, endpoint));
                }
            }

            return lost;
        }
    }

    private PeerState Heard(Participant participant)
    {
        PeerState peer = GetOrAdd(participant.Endpoint);
        peer.ParticipantId = participant.Id;
        Touch(peer);
        return peer;
    }

    // proof that peer is live
    private void Touch(PeerState peer)
    {
        peer.LastHeard = _time.GetUtcNow();
        peer.OwedSince = null;
        peer.Lost = false;
    }

    private void MarkLost(PeerState peer)
    {
        peer.Lost = true;
        peer.OwedSince = null;

        if (peer.SharedStreamId != null)
        {
            _sharerByStream.Remove(peer.SharedStreamId);
            peer.SharedStreamId = null;
        }
    }

    private void ForgetStream(string streamId)
    {
        if (_sharerByStream.Remove(streamId, out IPEndPoint? endpoint) &&
            _peers.TryGetValue(endpoint, out PeerState? peer) &&
            peer.SharedStreamId == streamId)
        {
            peer.SharedStreamId = null;
        }
    }

    private PeerState GetOrAdd(IPEndPoint endpoint)
    {
        if (!_peers.TryGetValue(endpoint, out PeerState? peer))
        {
            peer = new PeerState();
            _peers[endpoint] = peer;
        }

        return peer;
    }

    private sealed class PeerState
    {
        public string? ParticipantId { get; set; }
        public DateTimeOffset LastHeard { get; set; }
        public DateTimeOffset? OwedSince { get; set; }
        public string? SharedStreamId { get; set; }
        public bool Lost { get; set; }
    }
}
