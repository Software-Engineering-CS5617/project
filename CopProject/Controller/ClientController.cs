using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Networking;
using Utils;

namespace Controller;

// Client-side implementation of IControllerService .
// Runs on Machine B (or any participant machine). It sends JOIN/LEAVE messages to the remote host and listens for broadcast notifications
public sealed class ClientController : IControllerService
{
    private ControllerUser? _currentUser;
    private ICommunicator? _communicator;
    private bool _disposed;
    private string? _hostIp;
    private int _hostPort;

    //for storing the user count and users information
    private int _lastKnownUserCount;
    private readonly Dictionary<string,ControllerUser> _knownUsers = new();

    // Events
    public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;
    public event EventHandler<UserJoinedEventArgs>? UserJoined;
    public event EventHandler<UserLeftEventArgs>? UserLeft;

    public int ListenPort => _communicator?.ListenPort ?? 0;

    public ClientController(ControllerUser user, ICommunicator? communicator = null)
    {
        _currentUser = user ?? throw new ArgumentNullException(nameof(user));
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
    public bool JoinMeeting(string hostIp, int hostPort, ControllerUser user)
    {
        if (!IPAddress.TryParse(hostIp, out IPAddress? ipAddress))
        {
            Trace.TraceWarning($"JoinMeeting failed: '{hostIp}' is not a valid IP address.");
            return false;
        }
        var serverEndPoint = new IPEndPoint(IPAddress.Parse(hostIp),hostPort);
        if(_communicator != null){
            _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
            if(_communicator is IDisposable disposable){
                disposable.Dispose();
            }
            _communicator = null ;
        }

        ICommunicator? comm = CommunicatorFactory.CreateCommunicator(serverEndPoint,isServer:false,isClient:true);
        if(comm == null || comm.ListenPort == 0){
            (comm as IDisposable)?.Dispose() ;
            Trace.TraceWarning($"failed to connect to host at {hostIp}:{hostPort}");
            return false;
        }
        _communicator = comm ;
        _communicator.AddSubscriber(Protocol.ControllerSubscriberId, new ClientNotificationListener(this));
        _hostIp = hostIp;
        _hostPort = hostPort;
        _currentUser = user;
        user.IsHost = false;
        user.Ip = NetworkUtils.GetLocalIpAddress();
        user.Port = _communicator.ListenPort;
        user.JoinedAt = DateTime.UtcNow;
        lock (_knownUsers)
        {   
            _knownUsers[user.UserKey] = user;
        }
         string message = Protocol.FormatJoinRequest(user);
        SendToHost(hostIp, hostPort, message);
        Trace.TraceInformation($"Sent JOIN for user '{user.Username}' to {hostIp}:{hostPort}.");
        return true;
    }
    //Sends a LEAVE message to the host the client previously joined.
    public void LeaveMeeting(ControllerUser user)
    {
        if (_hostIp == null)
        {
            Trace.TraceWarning("LeaveMeeting called but no host has been joined yet.");
            return;
        }
        string userKey = _currentUser?.UserKey ?? user.UserKey;
        string message = Protocol.FormatLeaveRequest(userKey);
        SendToHost(_hostIp, _hostPort, message);
        Trace.TraceInformation($"Sent LEAVE for user '{user.Gmail}' to {_hostIp}:{_hostPort}.");

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
    public List<ControllerUser> GetActiveUsers()
    {
        lock (_knownUsers)
        {
            return _knownUsers.Values.ToList();
        }
    }

    // fire events from notification listener ─────────────

    internal void RaiseMeetingEnded(MeetingEndedEventArgs e)
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
        MeetingEnded?.Invoke(this, e);
    }
    internal void RaiseUserJoined(UserJoinedEventArgs e) => UserJoined?.Invoke(this, e);
    internal void RaiseUserLeft(UserLeftEventArgs e) => UserLeft?.Invoke(this, e);
    internal void UpdateUserCount(int count) => _lastKnownUserCount = count;
    internal void SyncUserList(IEnumerable<ControllerUser> users)
    {
        lock (_knownUsers)
        {
            _knownUsers.Clear();
            foreach (var user in users)
            {
                _knownUsers[user.UserKey]=user;
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
        if (_hostIp != null&& _currentUser != null)
        {
            try
            {
                string message = Protocol.FormatLeaveRequest(_currentUser.UserKey);
                SendToHost(_hostIp, _hostPort, message);
                Thread.Sleep(150);
                Trace.TraceInformation($"Dispose: auto-sent LEAVE for '{_currentUser.Username}' to {_hostIp}:{_hostPort}.");
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
                    try
                    {
                        // Payload: TargetUserJson | AllUsersListJson
                        ControllerUser? joinedUser = JsonSerializer.Deserialize<ControllerUser>(parsed.Payload);
                        if (joinedUser != null)
                        {
                            if (parsed.Extra.Length > 0 && !string.IsNullOrWhiteSpace(parsed.Extra[0]))
                            {
                                var allUsers = JsonSerializer.Deserialize<List<ControllerUser>>(parsed.Extra[0]);
                                if (allUsers != null)
                                    _client.SyncUserList(allUsers);
                            }
                            _client.UpdateUserCount(_client.GetActiveUserCount());
                            _client.RaiseUserJoined(new UserJoinedEventArgs(joinedUser, _client.GetActiveUserCount()));
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Failed to parse NotifyUserJoined: {ex.Message}");
                    }
                    break;
                }

            case Protocol.NotifyUserLeft:
                {
                    try
                    {
                        // Payload: TargetUserJson | AllUsersListJson
                        ControllerUser? leftUser = JsonSerializer.Deserialize<ControllerUser>(parsed.Payload);
                        if (leftUser != null)
                        {
                            if (parsed.Extra.Length > 0 && !string.IsNullOrWhiteSpace(parsed.Extra[0]))
                            {
                                var allUsers = JsonSerializer.Deserialize<List<ControllerUser>>(parsed.Extra[0]);
                                if (allUsers != null)
                                    _client.SyncUserList(allUsers);
                            }
                            _client.UpdateUserCount(_client.GetActiveUserCount());
                            _client.RaiseUserLeft(new UserLeftEventArgs(leftUser, _client.GetActiveUserCount()));
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Failed to parse NotifyUserLeft: {ex.Message}");
                    }
                    break;
                }

            case Protocol.NotifySessionEnded:
                {
                    try
                    {
                        ControllerUser? hostUser = JsonSerializer.Deserialize<ControllerUser>(parsed.Payload);
                        if (hostUser != null)
                        {
                            _client.RaiseMeetingEnded(new MeetingEndedEventArgs(hostUser));
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Failed to parse NotifySessionEnded: {ex.Message}");
                    }
                    break;
                }

            default:
                Trace.TraceWarning($"Client received unknown notification type: {parsed.Type}");
                break;
        }
    }
}
