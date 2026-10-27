using System.Diagnostics;
using Networking;

namespace Controller;

// Client-side implementation of IControllerService .
// Runs on Machine B (or any participant machine). It sends JOIN/LEAVE messages to the remote host and listens for broadcast notifications
public sealed class ClientController : IControllerService
{
    private readonly string _userId;
    private readonly ICommunicator _communicator;
    private bool _disposed;
    private string? _hostIp;
    private int _hostPort;

    //for storing the user count and users information
    private int _lastKnownUserCount;
    private readonly HashSet<string> _knownUsers = new();

    // Events
    public event EventHandler<SessionStartedEventArgs>? SessionStarted;
    public event EventHandler<SessionEndedEventArgs>? SessionEnded;
    public event EventHandler<UserJoinedEventArgs>? UserJoined;
    public event EventHandler<UserLeftEventArgs>? UserLeft;
    public event EventHandler<CountChangedEventArgs>? CountChanged;

    public int ListenPort => _communicator.ListenPort;

    public ClientController(string userId, ICommunicator communicator)
    {
        _userId = userId ?? throw new ArgumentNullException(nameof(userId));
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
        // Subscribe to incoming notifications from the host
        _communicator.AddSubscriber(Protocol.ControllerSubscriberId, new ClientNotificationListener(this));
    }
    // IControllerService functions implementation 

    // Not applicable on the client side. Use a HostController to start /end meetings.
    public bool StartMeeting()
    {
        Trace.TraceWarning("StartMeeting called on Client. Only the host can start meetings.");
        return false;
    }

    public bool EndMeeting()
    {
        Trace.TraceWarning("EndMeeting called on Client. Only the host can end meetings.");
        return false;
    }

    // Sends a JOIN message to the host machine after formatting.
    public void JoinMeeting(string hostIp, int hostPort, string userId)
    {
        // Remember the host so leave/users don't need the address again
        _hostIp = hostIp;
        _hostPort = hostPort;

        string message = Protocol.FormatJoinRequest(userId, NetworkUtils.GetLocalIpAddress(), _communicator.ListenPort);
        _communicator.SendMessage(hostIp, hostPort, Protocol.ControllerSubscriberId, message);
        Trace.TraceInformation($"Sent JOIN for user '{userId}' to {hostIp}:{hostPort}.");
    }

    //Sends a LEAVE message to the host the client previously joined.
    public void LeaveMeeting(string userId)
    {
        if (_hostIp == null)
        {
            Trace.TraceWarning("LeaveMeeting called but no host has been joined yet.");
            return;
        }

        string message = Protocol.FormatLeaveRequest(userId);
        _communicator.SendMessage(_hostIp, _hostPort, Protocol.ControllerSubscriberId, message);
        Trace.TraceInformation($"Sent LEAVE for user '{userId}' to {_hostIp}:{_hostPort}.");

        // Clear remembered host and local tracking
        _hostIp = null;
        _hostPort = 0;
        _lastKnownUserCount = 0;
        _knownUsers.Clear();
    }

    // Indicates whether the client is currently joined to a host
    public bool IsConnected => _hostIp != null;


    // Returns the user count tracked from host broadcast notifications.
    public int GetActiveUserCount()
    {
        return _knownUsers.Count > 0 ? _knownUsers.Count : _lastKnownUserCount;
    }

    /// Returns the  user list, updated from host broadcast notifications.
    public List<string> GetActiveUsers()
    {
        return _knownUsers.ToList();
    }

    // fire events from notification listener ─────────────

    internal void RaiseSessionStarted(SessionStartedEventArgs e) => SessionStarted?.Invoke(this, e);
    internal void RaiseSessionEnded(SessionEndedEventArgs e)
    {
        _hostIp = null;
        _hostPort = 0;
        _lastKnownUserCount = 0;
        _knownUsers.Clear();
        SessionEnded?.Invoke(this, e);
    }
    internal void RaiseUserJoined(UserJoinedEventArgs e) => UserJoined?.Invoke(this, e);
    internal void RaiseUserLeft(UserLeftEventArgs e) => UserLeft?.Invoke(this, e);
    internal void RaiseCountChanged(CountChangedEventArgs e) => CountChanged?.Invoke(this, e);

    // Updates the locally cached user count from broadcast notifications.
    internal void UpdateUserCount(int count) => _lastKnownUserCount = count;

    internal void SyncUserList(IEnumerable<string> userIds)
    {
        _knownUsers.Clear();
        foreach (string id in userIds)
            _knownUsers.Add(id);
    }

    public int GetLastKnownUserCount() => _lastKnownUserCount;

    // Cleanup 

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Auto-send LEAVE to host if still connected, so host cleans up this user
        if (_hostIp != null)
        {
            try
            {
                string message = Protocol.FormatLeaveRequest(_userId);
                _communicator.SendMessage(_hostIp, _hostPort, Protocol.ControllerSubscriberId, message);
                Trace.TraceInformation($"Dispose: auto-sent LEAVE for '{_userId}' to {_hostIp}:{_hostPort}.");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Dispose: failed to send LEAVE: {ex.Message}");
            }
        }

        _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
        _knownUsers.Clear();
    }
}

// Listens for broadcast notifications from the host and raises events .

internal class ClientNotificationListener : IMessageListener
{
    private readonly ClientController _client;

    public ClientNotificationListener(ClientController client)
    {
        _client = client;
    }

    public void OnMessageReceived(string message)
    {
        ParsedMessage? parsed = Protocol.Parse(message);
        if (parsed == null)
        {
            Trace.TraceWarning($"Client received malformed notification: {message}");
            return;
        }

        switch (parsed.Type)
        {
            case Protocol.NotifyUserJoined:
                {
                    // Wire format: USER_JOINED:userId:count
                    string userId = parsed.UserId;
                    int count = parsed.Extra.Length > 0 && int.TryParse(parsed.Extra[0], out int c) ? c : 0;
                    _client.RaiseUserJoined(new UserJoinedEventArgs(userId, count));
                    _client.RaiseCountChanged(new CountChangedEventArgs(count));
                    _client.UpdateUserCount(count);
                    break;
                }

            case Protocol.NotifyUserLeft:
                {
                    // Wire format: USER_LEFT:userId:count
                    string userId = parsed.UserId;
                    int count = parsed.Extra.Length > 0 && int.TryParse(parsed.Extra[0], out int c) ? c : 0;
                    _client.RaiseUserLeft(new UserLeftEventArgs(userId, count));
                    _client.RaiseCountChanged(new CountChangedEventArgs(count));
                    _client.UpdateUserCount(count);
                    break;
                }

            case Protocol.NotifySessionEnded:
                _client.RaiseSessionEnded(new SessionEndedEventArgs(parsed.UserId));
                break;

            case Protocol.NotifySessionStarted:
                _client.RaiseSessionStarted(new SessionStartedEventArgs(parsed.UserId));
                break;

            default:
                Trace.TraceWarning($"Client received unknown notification type: {parsed.Type}");
                break;
        }
    }
}