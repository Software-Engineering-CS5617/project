using System.Drawing;
using ScreenShare.Interfaces;
using ScreenShare.Models;

namespace ScreenShare;

/// <summary>
/// connects the ScreenShare UI to a ScreenShareNode: streams become tiles, visible tiles
/// become subscriptions, and new pictures become raw frames for the tiles.
///
/// this participant's own stream is not shown in their own grid.
/// events are raised on ScreenShare's thread; the view model moves them to the UI thread.
/// </summary>
public sealed class ScreenShareFeed : IScreenShareFeed, IDisposable
{
    private readonly ScreenShareNode _node;
    private readonly object _subscriptionLock = new();
    private HashSet<string> _subscribed = new();
    private bool _disposed;

    public event Action<string, string>? ParticipantJoined;

    public event Action<string>? ParticipantLeft;

    public event Action<string, ScreenFrame>? FrameReady;

    /// <param name="node">the node to show; it stays owned by the caller</param>
    /// <exception cref="ArgumentNullException">the node is null</exception>
    public ScreenShareFeed(ScreenShareNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        _node = node;
        _node.StreamAdded += OnStreamAdded;
        _node.StreamRemoved += OnStreamRemoved;
        _node.FrameUpdated += OnFrameUpdated;
    }

    /// <summary>
    /// stops passing on the node's events; the node itself stays owned by the caller
    /// </summary>
    public void Dispose()
    {
        lock (_subscriptionLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subscribed.Clear();
        }

        _node.StreamAdded -= OnStreamAdded;
        _node.StreamRemoved -= OnStreamRemoved;
        _node.FrameUpdated -= OnFrameUpdated;
    }

    public bool IsSharing => _node.IsSharing;

    public void StartSharing()
    {
        _node.StartSharing();
    }

    public void StopSharing()
    {
        _node.StopSharing();
    }

    /// <summary>
    /// subscribes to the streams that became visible and unsubscribes from the ones that
    /// did not stay visible. the requested resolution is ignored for now: every stream is
    /// sent at ScreenShareOptions.MaxHeight.
    /// </summary>
    public void RequestFeeds(IReadOnlyDictionary<string, int> idToResolution)
    {
        ArgumentNullException.ThrowIfNull(idToResolution);

        var wanted = new HashSet<string>(idToResolution.Keys);
        wanted.Remove(_node.LocalParticipant.Id);

        List<string> toSubscribe;
        List<string> toUnsubscribe;
        lock (_subscriptionLock)
        {
            if (_disposed)
            {
                return;
            }

            toSubscribe = wanted.Except(_subscribed).ToList();
            toUnsubscribe = _subscribed.Except(wanted).ToList();
            _subscribed = wanted;
        }

        foreach (string id in toUnsubscribe)
        {
            _node.Unsubscribe(id);
        }

        foreach (string id in toSubscribe)
        {
            _node.Subscribe(id);
        }
    }

    private bool IsOwnStream(string streamId)
    {
        return streamId == _node.LocalParticipant.Id;
    }

    private void OnStreamAdded(string streamId, string displayName)
    {
        if (!IsOwnStream(streamId))
        {
            ParticipantJoined?.Invoke(streamId, displayName);
        }
    }

    private void OnStreamRemoved(string streamId)
    {
        // the node drops the subscription itself, so forget it here too; a stream that
        // starts again is subscribed afresh when its tile is visible again
        lock (_subscriptionLock)
        {
            _subscribed.Remove(streamId);
        }

        if (!IsOwnStream(streamId))
        {
            ParticipantLeft?.Invoke(streamId);
        }
    }

    private void OnFrameUpdated(string streamId)
    {
        if (FrameReady == null)
        {
            return;
        }

        using Bitmap? frame = _node.GetFrame(streamId);
        if (frame != null)
        {
            FrameReady.Invoke(streamId, ScreenFrame.FromBitmap(frame));
        }
    }
}
