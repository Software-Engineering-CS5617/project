namespace Controller;

// Base class for all session-related event arguments.
public class SessionEventArgs : EventArgs
{
}

// It will be Raised when a user joins a session.
public class UserJoinedEventArgs : SessionEventArgs
{
    public string UserId { get; }
    public int NewCount { get; }

    public UserJoinedEventArgs(string userId, int newCount)
    {
        UserId = userId;
        NewCount = newCount;
    }
}

// It is Raised when a user leaves a session.
public class UserLeftEventArgs : SessionEventArgs
{
    public string UserId { get; }
    public int NewCount { get; }

    public UserLeftEventArgs(string userId, int newCount)
    {
        UserId = userId;
        NewCount = newCount;
    }
}

// It will be Raised when the active user count changes in a session.
public class CountChangedEventArgs : SessionEventArgs
{
    public int NewCount { get; }

    public CountChangedEventArgs(int newCount)
    {
        NewCount = newCount;
    }
}

// It will be  Raised when a session starts.It identifies the user who started the session.
public class SessionStartedEventArgs : SessionEventArgs
{
    public string HostUserId { get; }

    public SessionStartedEventArgs(string hostUserId)
    {
        HostUserId = hostUserId;
    }
}

// It will be Raised when a session ends.
public class SessionEndedEventArgs : SessionEventArgs
{
    public string EndedByUserId { get; }

    public SessionEndedEventArgs(string endedByUserId)
    {
        EndedByUserId = endedByUserId;
    }
}
