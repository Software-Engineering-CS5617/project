using System.Net;
using Screenshare;

namespace ScreenShare;

// sharing and message routing.
// the other half, setup, public API and shutdown, is in ScreenShareNode.cs.
public sealed partial class ScreenShareNode
{
    /// <summary>
    /// starts sharing this participant's screen
    /// </summary>
    /// <exception cref="ObjectDisposedException">the node was disposed</exception>
    public void StartSharing()
    {
        StartSharing(runCaptureLoop: true);
    }

    /// <summary>
    /// stops sharing; viewers are told the stream ended
    /// </summary>
    public void StopSharing()
    {
        ScreenShareSender? sender;
        lock (_sharingLock)
        {
            sender = _sender;
            Volatile.Write(ref _sender, null);
        }

        sender?.Dispose();
    }

    /// <param name="runCaptureLoop">false lets tests drive captures with CaptureOnce</param>
    internal void StartSharing(bool runCaptureLoop)
    {
        lock (_sharingLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_sender != null)
            {
                return;
            }

            var sender = new ScreenShareSender(
                LocalParticipant,
                _session.LocalUser.DisplayName,
                _imageSourceFactory(),
                SendToHost,
                _options,
                _timeProvider);
            Volatile.Write(ref _sender, sender);

            if (runCaptureLoop)
            {
                sender.StartLoop();
            }
        }
    }

    /// <summary>captures and sends one frame now; for tests</summary>
    internal bool CaptureOnce()
    {
        return Volatile.Read(ref _sender)?.CaptureOnce() ?? false;
    }

    /// <summary>runs the host's timeout check now; for tests</summary>
    internal void RunMaintenance()
    {
        if (_host != null)
        {
            _dispatcher.Post(_host.RemoveTimedOutViewers);
        }
    }

    /// <summary>
    /// sends a message from this participant's sender or receiver to the host's relay.
    /// safe from any thread.
    /// </summary>
    private void SendToHost(IScreenShareMessage message)
    {
        byte[] bytes = ScreenShareCodec.Encode(message);

        if (_host != null)
        {
            // queued rather than called directly, so the relay is never re-entered
            _dispatcher.Post(() => _host.Handle(message, bytes));
        }
        else
        {
            _communicator.SendMessage(_session.HostEndpoint, bytes, ScreenShareModule.ModuleId, ScreenShareModule.PriorityFor(message));
        }
    }

    /// <summary>
    /// runs on Networking's receive thread, so only queues the work
    /// </summary>
    private void OnNetworkMessage(IScreenShareMessage message, byte[] bytes)
    {
        if (_host != null)
        {
            _dispatcher.Post(() => _host.Handle(message, bytes));
        }
        else
        {
            _dispatcher.Post(() => HandleFromHost(message));
        }
    }

    /// <summary>
    /// handles a message the host sent to this participant (or, on the host, to itself)
    /// </summary>
    private void HandleFromHost(IScreenShareMessage message)
    {
        switch (message)
        {
            case FrameAck ack:
                ScreenShareSender? acked = Volatile.Read(ref _sender);
                if (acked != null && acked.StreamId == ack.StreamId)
                {
                    acked.OnFrameAck(ack.FrameNumber);
                }
                break;

            case RequestFullFrame request:
                ScreenShareSender? requested = Volatile.Read(ref _sender);
                if (requested != null && requested.StreamId == request.StreamId)
                {
                    requested.OnFullFrameRequested();
                }
                break;

            default:
                _receiver.Handle(message);
                break;
        }
    }

    /// <summary>
    /// routes the relay's output: the host's own endpoint is delivered in-process,
    /// everyone else goes through Networking
    /// </summary>
    private sealed class HostOutput : IRealyOutput
    {
        private readonly ScreenShareNode _node;

        public HostOutput(ScreenShareNode node)
        {
            _node = node;
        }

        public void Send(IPEndPoint to, IScreenShareMessage message, byte[] bytes)
        {
            if (to.Equals(_node.LocalParticipant.Endpoint))
            {
                _node._dispatcher.Post(() => _node.HandleFromHost(message));
            }
            else
            {
                _node._communicator.SendMessage(to, bytes, ScreenShareModule.ModuleId, ScreenShareModule.PriorityFor(message));
            }
        }

        public void Broadcast(IScreenShareMessage message, byte[] bytes)
        {
            // Networking's broadcast reaches every connected client but not the host itself
            _node._communicator.BroadcastMessage(bytes, ScreenShareModule.ModuleId, ScreenShareModule.PriorityFor(message));
            _node._dispatcher.Post(() => _node.HandleFromHost(message));
        }
    }
}
