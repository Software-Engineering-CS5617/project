using System.Diagnostics;
using System.Net;
using System.Text;
using Networking;

namespace Controller;

// Client-side implementation of IControllerService .
// Runs on Machine B (or any participant machine). It sends JOIN/LEAVE messages to the remote host and listens for broadcast notifications
public sealed class ClientController : IControllerService
{
    private readonly string _userId;
    private ICommunicator? _communicator;
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

    public int ListenPort => _communicator?.ListenPort ?? 0;

    public ClientController(string userId, ICommunicator? communicator = null)
    {
        _userId = userId ?? throw new ArgumentNullException(nameof(userId));
        _communicator = communicator;
        // Subscribe to incoming notifications from the host if communicator was provided upfront
        _communicator?.AddSubscriber(Protocol.ControllerSubscriberId, new ClientNotificationListener(this));
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

        if (_communicator == null)
        {
            var serverEndPoint = new IPEndPoint(IPAddress.Parse(hostIp), hostPort);
            _communicator = CommunicatorFactory.CreateCommunicator(serverEndPoint, isServer: false, isClient: true)
                ?? throw new InvalidOperationException($"Failed to connect to host at {hostIp}:{hostPort}.");
            _communicator.AddSubscriber(Protocol.ControllerSubscriberId, new ClientNotificationListener(this));
        }

        lock (_knownUsers)
        {
            _knownUsers.Add(userId);
        }

        string message = Protocol.FormatJoinRequest(userId, NetworkUtils.GetLocalIpAddress(), _communicator.ListenPort);
        SendToHost(hostIp, hostPort, message);
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
        SendToHost(_hostIp, _hostPort, message);
        Trace.TraceInformation($"Sent LEAVE for user '{userId}' to {_hostIp}:{_hostPort}.");

        // Allow background SendingQueueListenerClient thread to flush the LEAVE packet before closing socket
        Thread.Sleep(150);

        // Clear remembered host and local tracking
        _hostIp = null;
        _hostPort = 0;
        _lastKnownUserCount = 0;
        lock (_knownUsers)
        {
            _knownUsers.Clear();
        }

        if (_communicator != null)
        {
            _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
            if (_communicator is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _communicator = null;
        }
    }

    private void SendToHost(string hostIp, int hostPort, string message)
    {
        if (_communicator == null)
        {
            Trace.TraceWarning("SendToHost called but communicator is not initialized.");
            return;
        }

        var endPoint = new IPEndPoint(IPAddress.Parse(hostIp), hostPort);
        byte[] data = Encoding.UTF8.GetBytes(message);
        _communicator.SendMessage(endPoint, data, Protocol.ControllerSubscriberId, 1);
    }

    // Indicates whether the client is currently joined to a host
    public bool IsConnected => _hostIp != null;


    // Returns the user count tracked from host broadcast notifications.
    public int GetActiveUserCount()
    {
        lock (_knownUsers)
        {
            return _knownUsers.Count > 0 ? _knownUsers.Count : _lastKnownUserCount;
        }
    }

    /// Returns the  user list, updated from host broadcast notifications.
    public List<string> GetActiveUsers()
    {
        lock (_knownUsers)
        {
            return _knownUsers.ToList();
        }
    }

    // fire events from notification listener ─────────────

    internal void RaiseSessionStarted(SessionStartedEventArgs e) => SessionStarted?.Invoke(this, e);
    internal void RaiseSessionEnded(SessionEndedEventArgs e)
    {
        _hostIp = null;
        _hostPort = 0;
        _lastKnownUserCount = 0;
        lock (_knownUsers)
        {
            _knownUsers.Clear();
        }
        if (_communicator != null)
        {
            _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
            if (_communicator is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _communicator = null;
        }
        SessionEnded?.Invoke(this, e);
    }
    internal void RaiseUserJoined(UserJoinedEventArgs e) => UserJoined?.Invoke(this, e);
    internal void RaiseUserLeft(UserLeftEventArgs e) => UserLeft?.Invoke(this, e);
    internal void RaiseCountChanged(CountChangedEventArgs e) => CountChanged?.Invoke(this, e);

    // Updates the locally cached user count from broadcast notifications.
    internal void UpdateUserCount(int count) => _lastKnownUserCount = count;

    internal void AddKnownUser(string userId)
    {
        lock (_knownUsers)
        {
            _knownUsers.Add(userId);
        }
    }

    internal void RemoveKnownUser(string userId)
    {
        lock (_knownUsers)
        {
            _knownUsers.Remove(userId);
        }
    }

    internal void SyncUserList(IEnumerable<string> userIds)
    {
        lock (_knownUsers)
        {
            _knownUsers.Clear();
            foreach (string id in userIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _knownUsers.Add(id.Trim());
            }
        }
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
                SendToHost(_hostIp, _hostPort, message);
                Thread.Sleep(150);
                Trace.TraceInformation($"Dispose: auto-sent LEAVE for '{_userId}' to {_hostIp}:{_hostPort}.");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Dispose: failed to send LEAVE: {ex.Message}");
            }
        }

        if (_communicator != null)
        {
            _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
            if (_communicator is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _communicator = null;
        }

        lock (_knownUsers)
        {
            _knownUsers.Clear();
        }
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

    public void OnMessageReceived(byte[] message)
    {
        if (message == null || message.Length == 0)
        {
            Trace.TraceWarning("Client received empty notification.");
            return;
        }

        string messageString = Encoding.UTF8.GetString(message);
        ParsedMessage? parsed = Protocol.Parse(messageString);
        if (parsed == null)
        {
            Trace.TraceWarning($"Client received malformed notification: {messageString}");
            return;
        }

        switch (parsed.Type)
        {
            case Protocol.NotifyUserJoined:
                {
                    // Wire format: USER_JOINED:userId:count[:user1,user2,...]
                    string userId = parsed.UserId;
                    int count = parsed.Extra.Length > 0 && int.TryParse(parsed.Extra[0], out int c) ? c : 0;
                    _client.RaiseUserJoined(new UserJoinedEventArgs(userId, count));
                    _client.RaiseCountChanged(new CountChangedEventArgs(count));
                    _client.UpdateUserCount(count);

                    if (parsed.Extra.Length > 1 && !string.IsNullOrWhiteSpace(parsed.Extra[1]))
                    {
                        _client.SyncUserList(parsed.Extra[1].Split(',', StringSplitOptions.RemoveEmptyEntries));
                    }
                    else
                    {
                        _client.AddKnownUser(userId);
                    }
                    break;
                }

            case Protocol.NotifyUserLeft:
                {
                    // Wire format: USER_LEFT:userId:count[:user1,user2,...]
                    string userId = parsed.UserId;
                    int count = parsed.Extra.Length > 0 && int.TryParse(parsed.Extra[0], out int c) ? c : 0;
                    _client.RaiseUserLeft(new UserLeftEventArgs(userId, count));
                    _client.RaiseCountChanged(new CountChangedEventArgs(count));
                    _client.UpdateUserCount(count);

                    if (parsed.Extra.Length > 1 && !string.IsNullOrWhiteSpace(parsed.Extra[1]))
                    {
                        _client.SyncUserList(parsed.Extra[1].Split(',', StringSplitOptions.RemoveEmptyEntries));
                    }
                    else
                    {
                        _client.RemoveKnownUser(userId);
                    }
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
