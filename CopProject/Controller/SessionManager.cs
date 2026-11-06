using Utils;
namespace Controller;

// Tracks the single meeting session and its participants.
// Since each session is uniquely identified by its host IP:port pair,
// only one session is tracked at a time.
public class SessionManager
{
    private readonly object _lock = new();
    private SessionInfo? _session;

    //Raised when a session ends.
    public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;

    //Raised when a user joins the session.
    public event EventHandler<UserJoinedEventArgs>? UserJoined;

    //Raised when a user leaves the session.
    public event EventHandler<UserLeftEventArgs>? UserLeft;

    // Creates a new session with the given host.
    public SessionInfo? CreateSession(ControllerUser hostUser)
    {
        lock (_lock)
        {
            if (_session != null)
                return null;

            var session = new SessionInfo(hostUser);
            _session = session;
            return session;
        }
    }

    // Starts the meeting (transitions from Created -> Started).
    public bool StartSession(ControllerUser hostUser)
    {
        SessionInfo? session;
        lock (_lock)
        {
            session = _session;
            if (session == null)
                return false;

            if (!session.IsHost(hostUser.UserKey))
                return false;

            if (!session.Start())
                return false;
        }

        UserJoined?.Invoke(this, new UserJoinedEventArgs(session.HostUser, 1));
        return true;
    }

    // Ends the meeting. Only the host can end it.
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

        MeetingEnded?.Invoke(this, new MeetingEndedEventArgs(session.HostUser));
        return true;
    }

    // Adds a user to the active session.
    public bool JoinSession(ControllerUser user)
    {
        int newCount;
        lock (_lock)
        {
            if (_session == null)
                return false;

            if (!_session.AddUser(user))
                return false;

            newCount = _session.UserCount;
        }

        UserJoined?.Invoke(this, new UserJoinedEventArgs(user, newCount));
        return true;
    }

    // Removes a user from the session.
    public bool LeaveSession(string userKey)
    {
        int newCount;
        ControllerUser? removedUser;
        lock (_lock)
        {
            if (_session == null)
                return false;

            if (!_session.RemoveUser(userKey, out removedUser) || removedUser == null)
                return false;

            newCount = _session.UserCount;
        }

        UserLeft?.Invoke(this, new UserLeftEventArgs(removedUser, newCount));
        return true;
    }

    //Gets the active user count for the session.
    public int GetActiveUserCount()
    {
        lock (_lock)
        {
            if (!HasActiveSession()) return 0;
            return _session?.UserCount ?? 0;
        }
    }

    //Gets the user IDs of all participants in the session.
    public List<ControllerUser> GetActiveUsers()
    {
        lock (_lock)
        {
            if (!HasActiveSession()) return new List<ControllerUser>();
            return _session?.GetUsers() ?? new List<ControllerUser>();
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
