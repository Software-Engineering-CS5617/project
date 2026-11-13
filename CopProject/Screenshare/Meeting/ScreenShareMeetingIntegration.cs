using System.Diagnostics;
using Controller;
using Controller.Models;
using Microsoft.UI.Dispatching;
using ScreenShare.Views;

namespace ScreenShare;

/// <summary>
/// connects ScreenShare to the app's meeting: when a meeting starts it creates ScreenShare for
/// it and shows it in the view; when the meeting ends it shuts ScreenShare down before the
/// Controller closes the connection, and empties the view.
/// </summary>
public sealed class ScreenShareMeetingIntegration : IDisposable
{
    private readonly Func<IControllerService?> _activeController;
    private readonly MeetingSession _session;
    private readonly IScreenShareView _view;
    private readonly Action<Action> _runOnUi;
    private readonly Func<IImageSource>? _imageSourceFactory;
    private readonly ScreenShareOptions? _options;
    private readonly object _lock = new();

    private ControllerMeetingSession? _meeting;
    private ScreenShareNode? _node;
    private ScreenShareFeed? _feed;
    private bool _disposed;

    /// <param name="meetingService">the app's meeting service; gives the running meeting's controller</param>
    /// <param name="session">the app's meeting session; its start and end drive ScreenShare</param>
    /// <param name="view">the view in the meeting's Share panel</param>
    /// <param name="options">tuning; defaults to ScreenShareOptions' defaults</param>
    /// <exception cref="ArgumentNullException">an argument is null</exception>
    public ScreenShareMeetingIntegration(ControllerMeetingService meetingService, MeetingSession session, ScreenShareGridView view, ScreenShareOptions? options = null)
        : this(
            ActiveControllerOf(meetingService),
            session,
            view,
            RunOnDispatcher(view),
            imageSourceFactory: null,
            options)
    {
    }

    /// <param name="runOnUi">runs an action on the view's thread</param>
    internal ScreenShareMeetingIntegration(Func<IControllerService?> activeController,
        MeetingSession session,
        IScreenShareView view,
        Action<Action> runOnUi,
        Func<IImageSource>? imageSourceFactory,
        ScreenShareOptions? options)
    {
        ArgumentNullException.ThrowIfNull(activeController);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(runOnUi);

        _activeController = activeController;
        _session = session;
        _view = view;
        _runOnUi = runOnUi;
        _imageSourceFactory = imageSourceFactory;
        _options = options;

        _session.MeetingStarted += OnMeetingStarted;
        _session.MeetingEnding += OnMeetingEnding;
    }

    /// <summary>gets the running meeting's ScreenShare, or null between meetings</summary>
    internal ScreenShareNode? Node
    {
        get {
            lock (_lock)
            {
                return _node;
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _session.MeetingStarted -= OnMeetingStarted;
        _session.MeetingEnding -= OnMeetingEnding;
        StopMeeting();
    }

    private static Func<IControllerService?> ActiveControllerOf(ControllerMeetingService meetingService)
    {
        ArgumentNullException.ThrowIfNull(meetingService);

        return () => meetingService.ActiveController;
    }

    private static Action<Action> RunOnDispatcher(ScreenShareGridView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        DispatcherQueue ui = view.DispatcherQueue;
        return action => {
            if (ui.HasThreadAccess)
            {
                action();
            }
            else
            {
                ui.TryEnqueue(() => action());
            }
        };
    }

    private void OnMeetingStarted(MeetingInfo info)
    {
        // a meeting that was not ended properly is shut down first
        StopMeeting();

        ScreenShareFeed feed;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            ControllerMeetingSession? meeting = null;
            ScreenShareNode? node = null;
            try
            {
                IControllerService controller = _activeController()
                    ?? throw new InvalidOperationException("the meeting has no controller");
                meeting = new ControllerMeetingSession(controller, _session, info);
                node = new ScreenShareNode(meeting, _imageSourceFactory, _options);
                feed = new ScreenShareFeed(node);
            }
            catch (Exception e)
            {
                // ScreenShare failing must not stop the meeting; the Share panel stays empty
                Trace.WriteLine($"[ScreenShare] could not start for this meeting: {e.Message}");
                node?.Dispose();
                meeting?.Dispose();
                return;
            }

            _meeting = meeting;
            _node = node;
            _feed = feed;
        }

        _runOnUi(() => {
            // skip if the meeting already ended before the UI got here
            if (IsCurrentFeed(feed))
            {
                _view.Show(feed);
            }
        });
    }

    private void OnMeetingEnding()
    {
        StopMeeting();
    }

    private bool IsCurrentFeed(ScreenShareFeed feed)
    {
        lock (_lock)
        {
            return ReferenceEquals(_feed, feed);
        }
    }

    /// <summary>
    /// shuts ScreenShare down right away, on the calling thread, so it is done before the
    /// Controller closes the connection; only emptying the view waits for the UI thread
    /// </summary>
    private void StopMeeting()
    {
        ControllerMeetingSession? meeting;
        ScreenShareNode? node;
        ScreenShareFeed? feed;
        lock (_lock)
        {
            meeting = _meeting;
            node = _node;
            feed = _feed;
            _meeting = null;
            _node = null;
            _feed = null;
        }

        if (node == null)
        {
            return;
        }

        feed?.Dispose();
        node.Dispose();
        meeting?.Dispose();
        _runOnUi(_view.Clear);
    }
}
