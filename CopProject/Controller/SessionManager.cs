namespace Controller;

// Tracks the single meeting session and its participants.
// Since each session is uniquely identified by its host IP:port pair,
// only one session is tracked at a time.
public class SessionManager
{
    private readonly object _lock = new();
    private SessionInfo? _session;

    //Raised when a new session is created.
    public event EventHandler<SessionStartedEventArgs>? SessionStarted;

    //Raised when a session ends.
    public event EventHandler<SessionEndedEventArgs>? SessionEnded;

    //Raised when a user joins the session.
    public event EventHandler<UserJoinedEventArgs>? UserJoined;

    //Raised when a user leaves the session.
    public event EventHandler<UserLeftEventArgs>? UserLeft;

    //Raised whenever the active user count of the session changes.
    public event EventHandler<CountChangedEventArgs>? CountChanged;

    // Creates a new session with the given host.
    // The host is automatically added as the first participant.
    public SessionInfo? CreateSession(string hostUserId, PeerInfo hostPeer)
    {
        lock (_lock)
        {
            if (_session != null)
                return null;

            var session = new SessionInfo(hostUserId);
            session.AddPeer(hostPeer);
            _session = session;
            return session;
        }
    }

    // Starts the meeting (transitions from Created → Active).
    //True if the transition succeeded.
    public bool StartSession(string requestingUserId)
    {
        SessionInfo? session;
        lock (_lock)
        {
            session = _session;
            if (session == null)
                return false;

            if (!session.IsHost(requestingUserId))
                return false;

            if (!session.Start())
                return false;
        }

        // Raise event outside the lock to avoid deadlocks
        SessionStarted?.Invoke(this, new SessionStartedEventArgs(requestingUserId));
        return true;
    }

    // Ends the meeting. Only the host can end it.
    //True if the session was ended successfully.
    public bool EndSession(string requestingUserId)
    {
        SessionInfo? session;
        lock (_lock)
        {
            session = _session;
            if (session == null)
                return false;

            if (!session.IsHost(requestingUserId))
                return false;

            if (!session.End())
                return false;

            _session = null;
        }

        SessionEnded?.Invoke(this, new SessionEndedEventArgs(requestingUserId));
        return true;
    }

    /// Adds a user to the active session.
    // The session must exist and not be in the Ended state.
    //True if the user was added.
    public bool JoinSession(PeerInfo peer)
    {
        int newCount;
        lock (_lock)
        {
            if (_session == null)
                return false;

            if (!_session.AddPeer(peer))
                return false;

            newCount = _session.PeerCount;
        }

        UserJoined?.Invoke(this, new UserJoinedEventArgs(peer.UserId, newCount));
        CountChanged?.Invoke(this, new CountChangedEventArgs(newCount));
        return true;
    }

    // Removes a user from the session.
    //True if the user was found and removed.
    public bool LeaveSession(string userId)
    {
        int newCount;
        lock (_lock)
        {
            if (_session == null)
                return false;

            if (!_session.RemovePeer(userId))
                return false;

            newCount = _session.PeerCount;
        }

        UserLeft?.Invoke(this, new UserLeftEventArgs(userId, newCount));
        CountChanged?.Invoke(this, new CountChangedEventArgs(newCount));
        return true;
    }

    //Gets the active user count for the session.
    public int GetActiveUserCount()
    {
        lock (_lock)
        {
            if (!HasActiveSession()) return 0;
            return _session?.PeerCount ?? 0;
        }
    }

    //Gets the user IDs of all participants in the session.
    public List<string> GetActiveUsers()
    {
        lock (_lock)
        {
            if (!HasActiveSession()) return new List<string>();
            return _session?.GetUserIds() ?? new List<string>();
        }
    }

    //Gets all peers for the session. Used by host to broadcast.
    public List<PeerInfo> GetPeers()
    {
        lock (_lock)
        {
            return _session?.GetPeers() ?? new List<PeerInfo>();
        }
    }

    //Gets the session info, or null if no session exists.
    public SessionInfo? GetSession()
    {
        lock (_lock)
        {
            return _session;
        }
    }

    //true if there is an active (non-ended) session.
    public bool HasActiveSession()
    {
        lock (_lock)
        {
            return _session != null && _session.State != SessionState.Ended;
        }
    }

    // Clears the current session reference, allowing a new session to be created.
    public void ClearSession()
    {
        lock (_lock)
        {
            _session = null;
        }
    }
}
