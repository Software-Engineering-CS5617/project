using System.Diagnostics;
using System.Drawing;
using System.Net;
using Networking;

namespace ScreenShare;

/// <summary>
/// ScreenShare for one participant: one per app instance. on the host it also runs the relay.
///
/// every participant can view streams; any participant can share. all ScreenShare traffic goes
/// through the host, since Networking clients can only reach the host. the host cannot send to
/// itself over Networking, so its own sender and receiver talk to the relay in-process.
///
/// StreamAdded, StreamRemoved and FrameUpdated are raised on ScreenShare's own thread.
///
/// split into two files so two people can work on it:
/// - ScreenShareNode.cs: setup, public API and shutdown (this file)
/// - ScreenShareNode.Messaging.cs: sharing and message routing
/// </summary>
public sealed partial class ScreenShareNode : IDisposable
{
    private readonly IMeetingSession _session;
    private readonly ICommunicator _communicator;
    private readonly ScreenShareOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Func<IImageSource> _imageSourceFactory;
    private readonly SerialDispatcher _dispatcher;
    private readonly ScreenShareReceiver _receiver;
    private readonly ScreenShareHost? _host;
    private readonly ITimer? _maintenanceTimer;
    private readonly object _sharingLock = new();
    private ScreenShareSender? _sender;
    private bool _disposed;

    /// <summary>raised with the stream id and display name when someone starts sharing</summary>
    public event Action<string, string>? StreamAdded;

    /// <summary>raised with the stream id when someone stops sharing or leaves</summary>
    public event Action<string>? StreamRemoved;

    /// <summary>raised with the stream id when a subscribed stream's picture changed; read it with GetFrame</summary>
    public event Action<string>? FrameUpdated;

    /// <summary>gets this participant as other participants see it</summary>
    public Participant LocalParticipant { get; }

    /// <summary>gets whether this participant hosts the meeting and relays streams</summary>
    public bool IsHost => _host != null;

    /// <summary>gets whether this participant is sharing their screen</summary>
    public bool IsSharing => Volatile.Read(ref _sender) != null;

    /// <param name="session">the meeting, provided by the controller</param>
    /// <param name="imageSourceFactory">creates the capture source when sharing starts;
    /// defaults to the primary display scaled to MaxHeight</param>
    /// <param name="options">tuning; defaults to 24 FPS at 720p</param>
    /// <param name="timeProvider">clock for timeouts; defaults to the system clock</param>
    /// <exception cref="ArgumentNullException">the session is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">an option is invalid</exception>
    public ScreenShareNode(
        IMeetingSession session,
        Func<IImageSource>? imageSourceFactory = null,
        ScreenShareOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _communicator = session.Communicator;
        _options = options ?? new ScreenShareOptions();
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _imageSourceFactory = imageSourceFactory
            ?? (() => new ScalingImageSource(new ScreenCapturer(), _options.MaxHeight));

        // the endpoint of our own connection, which is what the host's Networking knows us by.
        // for the host it is the listening endpoint.
        LocalParticipant = new Participant(
            session.LocalUser.Id,
            new IPEndPoint(ToPlainAddress(_communicator.ListenIpAddress), _communicator.ListenPort));

        _dispatcher = new SerialDispatcher($"ScreenShare {session.LocalUser.Id}");
        _receiver = new ScreenShareReceiver(LocalParticipant, SendToHost, _timeProvider);
        _receiver.StreamAdded += (id, name) => StreamAdded?.Invoke(id, name);
        _receiver.StreamRemoved += id => StreamRemoved?.Invoke(id);
        _receiver.FrameUpdated += id => FrameUpdated?.Invoke(id);

        if (session.LocalUser.IsHost)
        {
            _host = new ScreenShareHost(LocalParticipant, new HostOutput(this), _options, _timeProvider);
            _maintenanceTimer = _timeProvider.CreateTimer(
                _ => _dispatcher.Post(() => _host.RemoveTimedOutViewers()),
                null,
                _options.MaintenanceInterval,
                _options.MaintenanceInterval);
        }

        _session.ParticipantLeft += OnParticipantLeft;
        _session.MeetingEnded += OnMeetingEnded;
        _communicator.AddSubscriber(ScreenShareModule.ModuleId, new ScreenShareMessageListener(OnNetworkMessage));

        if (_host == null)
        {
            // ask the host for the streams that are already running
            SendToHost(new Hello(LocalParticipant));
        }
    }

    /// <summary>
    /// starts receiving a stream, e.g. when its tile scrolls into view
    /// </summary>
    public void Subscribe(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        _dispatcher.Post(() => _receiver.Subscribe(streamId, _options.MaxHeight));
    }

    /// <summary>
    /// stops receiving a stream, e.g. when its tile scrolls out of view
    /// </summary>
    public void Unsubscribe(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        _dispatcher.Post(() => _receiver.Unsubscribe(streamId));
    }

    /// <summary>
    /// gets a copy of a subscribed stream's latest picture, or null. safe from any thread;
    /// the caller owns the returned bitmap.
    /// </summary>
    public Bitmap? GetFrame(string streamId)
    {
        return _receiver.GetFrame(streamId);
    }

    public void Dispose()
    {
        lock (_sharingLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _session.ParticipantLeft -= OnParticipantLeft;
        _session.MeetingEnded -= OnMeetingEnded;

        try
        {
            StopSharing();
            _communicator.RemoveSubscriber(ScreenShareModule.ModuleId);
        }
        catch (Exception e)
        {
            // the meeting's connection may already be closed
            Trace.WriteLine($"[ScreenShare] error while shutting down: {e.Message}");
        }

        _maintenanceTimer?.Dispose();

        // finishes queued work, then nothing else touches the relay or receiver
        _dispatcher.Dispose();
        _host?.Dispose();
        _receiver.Dispose();
    }

    /// <summary>gets the relay, on the host; for tests</summary>
    internal ScreenShareHost? Host => _host;

    /// <summary>true when no ScreenShare work is queued or running; for tests</summary>
    internal bool IsIdle => _dispatcher.IsIdle;

    /// <summary>
    /// a Networking client's socket is dual-mode, so it reports its IPv4 address in IPv6 form
    /// (::ffff:10.0.0.2), while the host's Networking stores clients by plain IPv4 (10.0.0.2).
    /// IPEndPoint treats those as different, and every send to us would fail with
    /// "client not found", so report the plain form. newer Networking also normalises
    /// these addresses itself; this stays so ScreenShare does not depend on that.
    /// </summary>
    private static IPAddress ToPlainAddress(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    private void OnParticipantLeft(MeetingParticipant participant)
    {
        if (_host != null)
        {
            _dispatcher.Post(() => _host.OnParticipantLeft(participant.Id));
        }
    }

    private void OnMeetingEnded()
    {
        Dispose();
    }
}
