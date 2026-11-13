using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Networking;
using Utils;
namespace Controller;

// Host-side implementation of IControllerService.
// Runs on Machine A — the machine that creates and owns meetings.It listens for incoming JOIN/LEAVE messages from clients, manages

public sealed class HostController : IControllerService
{
    private readonly ControllerUser _hostUser;
    private readonly ICommunicator _communicator;
    private readonly SessionManager _sessionManager;
    private bool _disposed;
    private readonly List<IUserNotificationHandler> _notificationHandlers = new();

    //Events 
    public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;
    public event EventHandler<UserJoinedEventArgs>? UserJoined;
    public event EventHandler<UserLeftEventArgs>? UserLeft;

    public int ListenPort => _communicator.ListenPort;
    public ICommunicator? Communicator => _communicator;
    public ControllerUser? CurrentUser => _hostUser;
    public bool IsMeetingActive => _sessionManager.HasActiveSession();

    public SessionManager SessionManager => _sessionManager;

    public void RegisterNotificationHandler(IUserNotificationHandler handler)
    {
        if(handler != null && !_notificationHandlers.Contains(handler))
            _notificationHandlers.Add(handler);
        
    }
    public void UnregisterNotificationHandler(IUserNotificationHandler handler)
    {
        if(handler != null && _notificationHandlers.Contains(handler))
            _notificationHandlers.Remove(handler);
    }
    public HostController(ControllerUser hostUser, ICommunicator communicator)
    {
        _hostUser = hostUser ?? throw new ArgumentNullException(nameof(hostUser));
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
        _sessionManager = new SessionManager();
        _hostUser.IsHost = true;
        _hostUser.Ip = NetworkUtils.GetLocalIpAddress();
        _hostUser.Port = _communicator.ListenPort;
        _communicator.AddSubscriber(Protocol.ControllerSubscriberId, new SessionMessageListener(_sessionManager));

        // Forward SessionManager events to our own event surface and broadcast to participants
        _sessionManager.UserJoined += OnUserJoined;
        _sessionManager.UserLeft += OnUserLeft;
        _sessionManager.MeetingEnded += OnMeetingEnded;
    }

    // IControllerService functions implementation 
    // Creates and starts a new meeting. The host user is added as the first participant.
    public bool StartMeeting()
    {
        SessionInfo? session = _sessionManager.CreateSession(_hostUser);
        if (session == null)
        {
            Trace.TraceWarning("StartMeeting failed: a session already exists.");
            return false;
        }

        bool started = _sessionManager.StartSession(_hostUser);
        if (!started)
        {
            Trace.TraceWarning("StartMeeting failed: could not transition session to Active.");
            return false;
        }
        foreach(var handler in _notificationHandlers)
        {
            handler.OnUserJoined(_hostUser);
        }

        Trace.TraceInformation($"Meeting started by host '{_hostUser.Username}'.");
        return true;
    }

    // Ends the meeting. Broadcasts a session-ended notification to all participants.
    public bool EndMeeting()
    {
        List<ControllerUser> users = _sessionManager.GetActiveUsers();

        bool ended = _sessionManager.EndSession(_hostUser.UserKey);
        if (!ended)
        {
            Trace.TraceWarning("EndMeeting failed.");
            return false;
        }
        
        // Broadcast end notification to all participants
        string payload = JsonSerializer.Serialize(_hostUser);
        Broadcast(Protocol.FormatNotification(Protocol.NotifySessionEnded, payload));
        Trace.TraceInformation($"Meeting ended by host '{_hostUser.Username}'.");
        return true;
    }

    // Not applicable on the host side
    public bool JoinMeeting(string hostIp, int hostPort, ControllerUser user)
    {
        Trace.TraceWarning("JoinMeeting called on HostController. Use a ClientController to join remote meetings.");
        return false;
    }

    public void LeaveMeeting(ControllerUser user)
    {
        Trace.TraceWarning("LeaveMeeting called on HostController. Use a ClientController to leave remote meetings.");
    }

    public int GetActiveUserCount() => _sessionManager.GetActiveUserCount();

    public List<ControllerUser> GetActiveUsers() => _sessionManager.GetActiveUsers();

    // Broadcasting 
    // Sends a message to every participant in the given peer list.
    private void Broadcast(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        _communicator.BroadcastMessage(data, Protocol.ControllerSubscriberId, 1);
    }

    // Event handlers 

    private void OnUserJoined(object? sender, UserJoinedEventArgs e)
    {
        UserJoined?.Invoke(this, e);
        List<ControllerUser> allUsers = _sessionManager.GetActiveUsers();
        string payload = $"{JsonSerializer.Serialize(e.User)}{Protocol.Separator}{JsonSerializer.Serialize(allUsers)}";
        Broadcast(Protocol.FormatNotification(Protocol.NotifyUserJoined, payload));
        foreach(var handler in _notificationHandlers)
        {
            handler.OnUserJoined(e.User);
        }
    }

    private void OnUserLeft(object? sender, UserLeftEventArgs e)
    {
        UserLeft?.Invoke(this, e);

        List<ControllerUser> allUsers = _sessionManager.GetActiveUsers();
        string payload = $"{JsonSerializer.Serialize(e.User)}{Protocol.Separator}{JsonSerializer.Serialize(allUsers)}";
        Broadcast(Protocol.FormatNotification(Protocol.NotifyUserLeft, payload));
        foreach(var handler in _notificationHandlers)
        {
            handler.OnUserLeft(e.User);
        }
    }

    private void OnMeetingEnded(object? sender, MeetingEndedEventArgs e)
    {
        MeetingEnded?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sessionManager.UserJoined -= OnUserJoined;
        _sessionManager.UserLeft -= OnUserLeft;
        _sessionManager.MeetingEnded -= OnMeetingEnded;

        _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
        Thread.Sleep(150);
        (_communicator as IDisposable)?.Dispose();
    }
}
