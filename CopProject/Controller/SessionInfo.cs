using Utils;
namespace Controller;

// Holds all metadata about a single meeting session, including its lifecycle
// state, the host who created it, and all current participants(ControllerUser's).
public sealed class SessionInfo
{
    private readonly Dictionary<string, ControllerUser> _users = new();
    private readonly object _lock = new();

    public ControllerUser HostUser { get; }
    public DateTime CreatedAt { get; }
    public SessionState State { get; private set; }

    public SessionInfo(ControllerUser hostUser)
    {
        HostUser = hostUser ?? throw new ArgumentNullException(nameof(hostUser));
        CreatedAt = DateTime.UtcNow;
        State = SessionState.Created;
        // Add host as the first participant
        _users[hostUser.UserKey] = hostUser;
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
    public bool AddUser(ControllerUser user)
    {
        lock (_lock)
        {
            if (State == SessionState.Ended) return false;
            if (_users.ContainsKey(user.UserKey)) return false;
            _users[user.UserKey] = user;
            return true;
        }
    }

    //Removes a participant.
    //True if the user was found and removed.
    public bool RemoveUser(string userKey, out ControllerUser? removedUser)
    {
        lock (_lock)
        {
            if (_users.TryGetValue(userKey, out removedUser))
            {
                return _users.Remove(userKey);
            }
            removedUser = null;
            return false;
        }
    }

    //Gets the current number of participants.
    public int UserCount
    {
        get { lock (_lock) { return _users.Count; } }
    }

    //Gets a snapshot of all current participant user IDs.
    public List<ControllerUser> GetUsers()
    {
        lock (_lock)
        {
            return _users.Values.ToList();
        }
    }

    //Checks whether the given user is the host of this session.
    public bool IsHost(string userKey) => HostUser.UserKey == userKey;
}
