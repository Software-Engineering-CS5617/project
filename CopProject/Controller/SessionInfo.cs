namespace Controller;

// Holds all metadata about a single meeting session, including its lifecycle
// state, the host who created it, and all current participants with their
// network addresses (PeerInfo).
public sealed class SessionInfo
{
    private readonly Dictionary<string, PeerInfo> _peers = new();
    private readonly object _lock = new();

    public string HostUserId { get; }
    public DateTime CreatedAt { get; }
    public SessionState State { get; private set; }

    public SessionInfo(string hostUserId)
    {
        HostUserId = hostUserId ?? throw new ArgumentNullException(nameof(hostUserId));
        CreatedAt = DateTime.UtcNow;
        State = SessionState.Created;
    }

    //Transitions the session to the Active state.
    //True if the transition was valid (from Created), false otherwise.
    public bool Start()
    {
        lock (_lock)
        {
            if (State != SessionState.Created) return false;
            State = SessionState.Active;
            return true;
        }
    }

    //Transitions the session to the Ended state.
    //True if the transition was valid (from Created or Active), false otherwise.
    public bool End()
    {
        lock (_lock)
        {
            if (State == SessionState.Ended) return false;
            State = SessionState.Ended;
            return true;
        }
    }

    //Adds a participant with their network address.
    //True if the user was added
    public bool AddPeer(PeerInfo peer)
    {
        lock (_lock)
        {
            if (State == SessionState.Ended) return false;
            if (_peers.ContainsKey(peer.UserId)) return false;
            _peers[peer.UserId] = peer;
            return true;
        }
    }

    //Removes a participant.
    //True if the user was found and removed.
    public bool RemovePeer(string userId)
    {
        lock (_lock)
        {
            return _peers.Remove(userId);
        }
    }

    //Gets the current number of participants.
    public int PeerCount
    {
        get { lock (_lock) { return _peers.Count; } }
    }

    //Gets a snapshot of all current participant user IDs.
    public List<string> GetUserIds()
    {
        lock (_lock)
        {
            return _peers.Keys.ToList();
        }
    }

    //Gets a snapshot of all current participants with their network info.
    public List<PeerInfo> GetPeers()
    {
        lock (_lock)
        {
            return _peers.Values.ToList();
        }
    }

    //Checks whether the given user is the host of this session.
    public bool IsHost(string userId) => HostUserId == userId;
}
