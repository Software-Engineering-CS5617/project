namespace ScreenShare;

/// <summary>what a client should do after one sweep of ClientLinkMonitor</summary>
/// <param name="Outgoing">messages to send to the host</param>
/// <param name="GoneStreams">streams that ended while the client was offline. deliver StreamStopped locally</param>
/// <param name="Connected">non-null when the link to the host just went down (false) or came back (true)</param>
public sealed record ClientActions(
    IReadOnlyList<IScreenShareMessage> Outgoing,
    IReadOnlyList<string> GoneStreams,
    bool? Connected);

/// <summary>
/// client notices that the host went quiet or dropped this client and resends the messages. 
/// A subscribed stream that delivers nothing is resubscribed
/// A sharer whose frames are never acknowledged re-announces its stream
/// After the host was silent, Hello is sent until it answers   
/// </summary>
public sealed class ClientLinkMonitor : ITrafficObserver
{
    private readonly ResilienceOptions _options;
    private readonly TimeProvider _time;
    private readonly object _lock = new();
    /// <summary>
    /// streams the client is subscribed to, and when it last got a frame from them
    /// </summary>
    private readonly Dictionary<string, SubscriptionState> _subscriptions = new();
    /// <summary>
    /// streams the host has announced, so the client can tell which ones ended while it was offline
    /// </summary>
    private readonly HashSet<string> _knownStreams = new();

    /// <summary>
    /// client's last Hello, so it can resend it to the host if the host went silent
    /// </summary>
    private Hello? _hello;

    /// <summary>
    /// client's last StreamStarted, so it can resend it to the host if the host went silent
    /// </summary>
    private StreamStarted? _announce;
    private DateTimeOffset _lastHostHeard;
    private DateTimeOffset? _unackedSince;

    /// <summary>
    /// so that retries are done in a limited manner
    /// </summary>
    private DateTimeOffset _nextAnnounceAt;
    private DateTimeOffset _nextProbeAt;
    private bool _connected = true;
    private bool _recovering;

    /// <summary>
    /// period during which the host is expected to re-announce its streams after a reconnect
    /// </summary>
    private DateTimeOffset? _reconcileUntil;
    private HashSet<string>? _seenDuringReconcile;

    public ClientLinkMonitor(ResilienceOptions options, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(time);

        _options = options;
        _time = time;
        _lastHostHeard = time.GetUtcNow();
    }

    /// <summary>gets whether the host has been heard from recently enough</summary>
    public bool IsConnected
    {
        get {
            lock (_lock)
            {
                return _connected;
            }
        }
    }

    // only a client that is subscribed or sharing has a reason to hear from the host
    private bool Expecting => _subscriptions.Count > 0 || _announce != null;

    /// <summary> host sent something </summary>
    /// <param name="message"></param>
    public void OnInbound(byte[] message)
    {
        if (!WireFormat.TryPeekKind(message, out ScreenShareMessageKind kind))
        {
            return;
        }

        string? dataStreamId = null;
        IScreenShareMessage? decoded = null;

        if (kind == ScreenShareMessageKind.Data)
        {
            WireFormat.TryPeekStreamId(message, out dataStreamId);
        }
        else
        {
            ScreenShareCodec.Decode(message, out decoded);
        }

        lock (_lock)
        {
            DateTimeOffset now = _time.GetUtcNow();
            _lastHostHeard = now;

            if (!_connected)
            {
                _connected = true;
                _recovering = true;

                // start collecting the host's announcements now, so this message that
                // revealed the link is back is counted too
                _reconcileUntil = now + _options.ReconcileWindow;
                _seenDuringReconcile = new HashSet<string>();
            }

            if (dataStreamId != null)
            {
                if (_subscriptions.TryGetValue(dataStreamId, out SubscriptionState? data))
                {
                    data.LastData = now;
                    data.Attempts = 0;
                }

                return;
            }

            // control messages
            switch (decoded)
            {
                case StreamStarted started:
                    _knownStreams.Add(started.StreamId);
                    _seenDuringReconcile?.Add(started.StreamId);

                    // the host restarted the stream's numbering and will send a full frame
                    if (_subscriptions.TryGetValue(started.StreamId, out SubscriptionState? restarted))
                    {
                        restarted.LastData = now;
                    }
                    break;

                case StreamStopped stopped:
                    _knownStreams.Remove(stopped.StreamId);
                    _subscriptions.Remove(stopped.StreamId);
                    break;

                case FrameAck ack when _announce != null && ack.StreamId == _announce.StreamId:
                    _unackedSince = null;
                    break;
            }
        }
    }

    /// <summary> client sends to host </summary>
    /// <param name="to"></param>
    /// <param name="message"></param>
    public void OnOutbound(System.Net.IPEndPoint to, byte[] message)
    {
        if (!WireFormat.TryPeekKind(message, out ScreenShareMessageKind kind))
        {
            return;
        }

        if (kind == ScreenShareMessageKind.Data)
        {
            lock (_lock)
            {
                if (_announce != null)
                {
                    _unackedSince ??= _time.GetUtcNow();
                }
            }

            return;
        }

        if (!ScreenShareCodec.Decode(message, out IScreenShareMessage? decoded))
        {
            return;
        }

        lock (_lock)
        {
            DateTimeOffset now = _time.GetUtcNow();
            bool wasExpecting = Expecting;

            switch (decoded)
            {
                case Hello hello:
                    _hello = hello;
                    break;

                case Subscribe subscribe:
                    if (_subscriptions.TryGetValue(subscribe.StreamId, out SubscriptionState? existing))
                    {
                        // also seen when this monitor resends it; attempts are kept so backoff holds
                        existing.Message = subscribe;
                        existing.LastData = now;
                    }
                    else
                    {
                        _subscriptions[subscribe.StreamId] = new SubscriptionState(subscribe, now);
                    }
                    break;

                case Unsubscribe unsubscribe:
                    _subscriptions.Remove(unsubscribe.StreamId);
                    break;

                case StreamStarted started:
                    _announce = started;
                    _unackedSince = null;
                    _nextAnnounceAt = now + _options.RecoveryInterval;
                    break;

                case StreamStopped stopped:
                    if (_announce?.StreamId == stopped.StreamId)
                    {
                        _announce = null;
                        _unackedSince = null;
                    }
                    break;
            }

            // the silence clock starts when there is first something to wait for
            if (!wasExpecting && Expecting)
            {
                _lastHostHeard = now;
            }
        }
    }

    public bool ShouldSend(System.Net.IPEndPoint to)
    {
        return true;
    }

    public ClientActions Sweep()
    {
        var outgoing = new List<IScreenShareMessage>();
        var gone = new List<string>();
        bool? connectionChanged = null;

        lock (_lock)
        {
            DateTimeOffset now = _time.GetUtcNow();

            if (_recovering)
            {
                // the host answered after a silence so rebuild everything it may have forgotten
                _recovering = false;
                connectionChanged = true;

                if (_hello != null)
                {
                    outgoing.Add(_hello);
                }

                foreach (SubscriptionState subscription in _subscriptions.Values)
                {
                    outgoing.Add(subscription.Message);
                    subscription.Attempts = 0;
                    subscription.LastData = now;
                    subscription.NextAttempt = now + _options.RecoveryInterval;
                }

                if (_announce != null)
                {
                    outgoing.Add(_announce);
                    _unackedSince = now;
                    _nextAnnounceAt = now + _options.RecoveryInterval;
                }
            }
            else if (!_connected)
            {
                // the host may have written this client off in which case it sends nothing
                // until it hears from it, so keep knocking
                if (_hello != null && now >= _nextProbeAt)
                {
                    outgoing.Add(_hello);
                    _nextProbeAt = now + _options.RecoveryInterval;
                }
            }
            else if (Expecting && now - _lastHostHeard > _options.HostSilenceTimeout)
            {
                _connected = false;
                _nextProbeAt = now;
                connectionChanged = false;
            }
            else
            {
                // healthy
                ResubscribeStalledStreams(now, outgoing);
                ReannounceUnackedStream(now, outgoing);
            }

            if (_connected && _reconcileUntil is DateTimeOffset until && now >= until)
            {
                foreach (string streamId in _knownStreams.Where(id => !_seenDuringReconcile!.Contains(id)).ToList())
                {
                    _knownStreams.Remove(streamId);
                    _subscriptions.Remove(streamId);
                    gone.Add(streamId);
                }

                _reconcileUntil = null;
                _seenDuringReconcile = null;
            }
        }

        return new ClientActions(outgoing, gone, connectionChanged);
    }

    /// <summary>
    /// resends subscribe if the host has not sent a frame for it in a while
    /// </summary>
    /// <param name="now"></param>
    /// <param name="outgoing"></param>
    private void ResubscribeStalledStreams(DateTimeOffset now, List<IScreenShareMessage> outgoing)
    {
        foreach (SubscriptionState subscription in _subscriptions.Values)
        {
            if (now - subscription.LastData > _options.StreamStallTimeout &&
                now >= subscription.NextAttempt &&
                subscription.Attempts < _options.MaxRecoveryAttempts)
            {
                outgoing.Add(subscription.Message);
                subscription.Attempts++;
                subscription.NextAttempt = now + _options.RecoveryInterval * subscription.Attempts;
            }
        }
    }

    // frames were sent but the host acknowledged none of them: it has probably forgotten this stream
    private void ReannounceUnackedStream(DateTimeOffset now, List<IScreenShareMessage> outgoing)
    {
        if (_announce != null &&
            _unackedSince is DateTimeOffset since &&
            now - since > _options.StreamStallTimeout &&
            now >= _nextAnnounceAt)
        {
            outgoing.Add(_announce);
            _unackedSince = now;
            _nextAnnounceAt = now + _options.RecoveryInterval;
        }
    }

    private sealed class SubscriptionState
    {
        public SubscriptionState(Subscribe message, DateTimeOffset now)
        {
            Message = message;
            LastData = now;
        }

        public Subscribe Message { get; set; }
        public DateTimeOffset LastData { get; set; }
        public int Attempts { get; set; }
        public DateTimeOffset NextAttempt { get; set; }
    }
}
