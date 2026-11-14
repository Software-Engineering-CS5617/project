using System.Drawing;
using ScreenShare;

namespace ScreenShare;

internal sealed class ScreenShareReceiver : IDisposable
{
    private readonly Participant _local;
    private readonly Action<IScreenShareMessage> _sendToHost;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, string> _streamNames = new();
    private readonly Dictionary<string, Subscription> _subscriptions = new();
    private readonly StreamFrameCache _frames = new();

    /// <summary>raised with the stream id and display name when a stream starts</summary>
    public event Action<string, string>? StreamAdded;

    /// <summary>raised with the stream id when a stream stops</summary>
    public event Action<string>? StreamRemoved;

    /// <summary>raised with the stream id when a subscribed stream's picture changed</summary>
    public event Action<string>? FrameUpdated;

    /// <param name="local">this participant, sent with every request so the host can reply</param>
    /// <param name="sendToHost">sends a message to the host</param>
    public ScreenShareReceiver(
        Participant local,
        Action<IScreenShareMessage> sendToHost,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(sendToHost);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _local = local;
        _sendToHost = sendToHost;
        _timeProvider = timeProvider;
    }

    private Subscription CreateSubscription(string streamId)
    {
        Subscription subscription = new Subscription(streamId, _timeProvider);
        subscription.Applier.FullFrameRequested += id => _sendToHost(new RequestFullFrame(_local, id));
        return subscription;
    }

    private bool RemoveSubscription(string streamId)
    {
        if (!_subscriptions.Remove(streamId, out Subscription? subscription))
        {
            return false;
        }

        subscription.Dispose();
        _frames.Remove(streamId);
        return true;
    }

    /// <summary>
    /// starts receiving a stream. the host replies with a full frame from its copy.
    /// unknown streams and existing subscriptions are ignored.
    /// </summary>
    public void Subscribe(string streamId, int maxHeight)
    {
        if (!_streamNames.ContainsKey(streamId) || _subscriptions.ContainsKey(streamId))
        {
            return;
        }

        _subscriptions[streamId] = CreateSubscription(streamId);
        _sendToHost(new Subscribe(_local, streamId, maxHeight));
    }

    /// <summary>
    /// stops receiving a stream and forgets its picture
    /// </summary>
    public void Unsubscribe(string streamId)
    {
        if (!RemoveSubscription(streamId))
        {
            return;
        }

        _sendToHost(new Unsubscribe(_local, streamId));
    }

    /// <summary>
    /// gets a copy of a stream's latest image, or null.
    /// </summary>
    public Bitmap? GetFrame(string streamId)
    {
        return _frames.Get(streamId);
    }

    private void OnStreamStarted(StreamStarted started)
    {
        bool isNew = !_streamNames.ContainsKey(started.StreamId);
        _streamNames[started.StreamId] = started.DisplayName;

        // a restarted stream numbers its frames from 0 again, so start a fresh picture;
        // the host sends this subscriber a full frame
        if (_subscriptions.Remove(started.StreamId, out Subscription? old))
        {
            old.Dispose();
            _subscriptions[started.StreamId] = CreateSubscription(started.StreamId);
        }

        if (isNew)
        {
            StreamAdded?.Invoke(started.StreamId, started.DisplayName);
        }
    }

    private void OnData(ScreenDataPacket packet)
    {
        if (!_subscriptions.TryGetValue(packet.StreamId, out Subscription? subscription))
        {
            return;
        }

        // acknowledge receipt even if the packet turns out unusable, so the host keeps pacing
        _sendToHost(new FrameAck(_local, packet.StreamId, packet.FrameNumber));

        PacketApplyOutcome outcome = subscription.Applier.Apply(packet);
        if (outcome != PacketApplyOutcome.Applied || packet.IsPulse)
        {
            return;
        }

        using (Bitmap frame = subscription.Stitcher.GetCurrentFrame()!)
        {
            _frames.Update(packet.StreamId, frame);
        }

        FrameUpdated?.Invoke(packet.StreamId);
    }

    /// <summary>
    /// handles a message the host sent to this participant
    /// </summary>
    public void Handle(IScreenShareMessage message)
    {
        switch (message)
        {
            case StreamStarted started:
                OnStreamStarted(started);
                break;

            case StreamStopped stopped:
                RemoveSubscription(stopped.StreamId);
                if (_streamNames.Remove(stopped.StreamId))
                {
                    StreamRemoved?.Invoke(stopped.StreamId);
                }
                break;

            case ScreenDataPacket packet:
                OnData(packet);
                break;
        }
    }

    public void Dispose()
    {
        foreach (Subscription subscription in _subscriptions.Values)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _frames.Dispose();
    }

    /// <summary>
    /// implemented for internal use
    /// </summary>
    private sealed class Subscription : IDisposable
    {
        public ImageStitcher Stitcher { get; } = new();
        public ScreenPacketApplier Applier { get; }

        public Subscription(string streamId, TimeProvider timeProvider)
        {
            Applier = new ScreenPacketApplier(streamId, Stitcher, timeProvider);
        }

        public void Dispose()
        {
            Stitcher.Dispose();
        }
    }
}
