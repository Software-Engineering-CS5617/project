using System.Diagnostics;
using System.Net;
using System.Text;
using Networking;

namespace Controller;

// Host-side implementation of IControllerService.
// Runs on Machine A — the machine that creates and owns meetings.It listens for incoming JOIN/LEAVE messages from clients, manages

public sealed class HostController : IControllerService
{
    private readonly string _hostUserId;
    private readonly ICommunicator _communicator;
    private readonly SessionManager _sessionManager;
    private bool _disposed;

    //Events 
    public event EventHandler<SessionStartedEventArgs>? SessionStarted;
    public event EventHandler<SessionEndedEventArgs>? SessionEnded;
    public event EventHandler<UserJoinedEventArgs>? UserJoined;
    public event EventHandler<UserLeftEventArgs>? UserLeft;
    public event EventHandler<CountChangedEventArgs>? CountChanged;

    public int ListenPort => _communicator.ListenPort;

    public bool IsMeetingActive => _sessionManager.HasActiveSession();

    public SessionManager SessionManager => _sessionManager;

    public HostController(string hostUserId, ICommunicator communicator)
    {
        _hostUserId = hostUserId ?? throw new ArgumentNullException(nameof(hostUserId));
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
        _sessionManager = new SessionManager();

        _communicator.AddSubscriber(Protocol.ControllerSubscriberId, new SessionMessageListener(_sessionManager));

        // Forward SessionManager events to our own event surface and broadcast to participants
        _sessionManager.UserJoined += OnUserJoined;
        _sessionManager.UserLeft += OnUserLeft;
        _sessionManager.CountChanged += OnCountChanged;
        _sessionManager.SessionStarted += OnSessionStarted;
        _sessionManager.SessionEnded += OnSessionEnded;
    }

    // IControllerService functions implementation 
    // Creates and starts a new meeting. The host user is added as the first participant.
    public bool StartMeeting()
    {
        var hostPeer = new PeerInfo(_hostUserId, NetworkUtils.GetLocalIpAddress(), _communicator.ListenPort);

        SessionInfo? session = _sessionManager.CreateSession(_hostUserId, hostPeer);
        if (session == null)
        {
            Trace.TraceWarning("StartMeeting failed: a session already exists.");
            return false;
        }

        bool started = _sessionManager.StartSession(_hostUserId);
        if (!started)
        {
            Trace.TraceWarning("StartMeeting failed: could not transition session to Active.");
            return false;
        }

        Trace.TraceInformation($"Meeting started by host '{_hostUserId}'.");
        return true;
    }

    // Ends the meeting. Broadcasts a session-ended notification to all participants.
    public bool EndMeeting()
    {
        // Get peers before ending so we can notify them
        List<PeerInfo> peers = _sessionManager.GetPeers();

        bool ended = _sessionManager.EndSession(_hostUserId);
        if (!ended)
        {
            Trace.TraceWarning("EndMeeting failed.");
            return false;
        }
        
        // Broadcast end notification to all participants
        BroadcastToSession(peers, Protocol.FormatNotification(Protocol.NotifySessionEnded, _hostUserId));
        Trace.TraceInformation($"Meeting ended by host '{_hostUserId}'.");
        return true;
    }

    // Not applicable on the host side
    public void JoinMeeting(string hostIp, int hostPort, string userId)
    {
        Trace.TraceWarning("JoinMeeting called on HostController. Use a ClientController to join remote meetings.");
    }

    public void LeaveMeeting(string userId)
    {
        Trace.TraceWarning("LeaveMeeting called on HostController. Use a ClientController to leave remote meetings.");
    }

    public int GetActiveUserCount() => _sessionManager.GetActiveUserCount();

    public List<string> GetActiveUsers() => _sessionManager.GetActiveUsers();

    // Broadcasting 
    // Sends a message to every participant in the given peer list.
    private void BroadcastToSession(List<PeerInfo> peers, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (PeerInfo peer in peers)
        {
            // Do not send broadcast packets to the host itself
            if (peer.Port == _communicator.ListenPort) continue;

            try
            {
                var endPoint = new IPEndPoint(IPAddress.Parse(peer.IpAddress), peer.Port);
                _communicator.SendMessage(endPoint, data, Protocol.ControllerSubscriberId, 1);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to send to {peer}: {ex.Message}");
            }
        }
    }

    // Event handlers 

    private void OnUserJoined(object? sender, UserJoinedEventArgs e)
    {
        UserJoined?.Invoke(this, e);

        List<PeerInfo> peers = _sessionManager.GetPeers();
        string userList = string.Join(",", _sessionManager.GetActiveUsers());
        BroadcastToSession(peers, Protocol.FormatNotification(Protocol.NotifyUserJoined, $"{e.UserId}:{e.NewCount}:{userList}"));
    }

    private void OnUserLeft(object? sender, UserLeftEventArgs e)
    {
        UserLeft?.Invoke(this, e);

        List<PeerInfo> peers = _sessionManager.GetPeers();
        string userList = string.Join(",", _sessionManager.GetActiveUsers());
        BroadcastToSession(peers, Protocol.FormatNotification(
            Protocol.NotifyUserLeft, $"{e.UserId}:{e.NewCount}:{userList}"));
    }

    private void OnCountChanged(object? sender, CountChangedEventArgs e)
    {
        CountChanged?.Invoke(this, e);
    }

    private void OnSessionStarted(object? sender, SessionStartedEventArgs e)
    {
        SessionStarted?.Invoke(this, e);
    }

    private void OnSessionEnded(object? sender, SessionEndedEventArgs e)
    {
        SessionEnded?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sessionManager.UserJoined -= OnUserJoined;
        _sessionManager.UserLeft -= OnUserLeft;
        _sessionManager.CountChanged -= OnCountChanged;
        _sessionManager.SessionStarted -= OnSessionStarted;
        _sessionManager.SessionEnded -= OnSessionEnded;

        _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
    }
}
