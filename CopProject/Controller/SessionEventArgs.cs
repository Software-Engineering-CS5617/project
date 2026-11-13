using Utils;
namespace Controller;

// Base class for all session-related event arguments.
public class SessionEventArgs : EventArgs
{
}

// It will be Raised when a user joins a session.
public class UserJoinedEventArgs : SessionEventArgs
{
    public ControllerUser User { get; }
    public int NewCount { get; }

    public UserJoinedEventArgs(ControllerUser user, int newCount)
    {
        User = user;
        NewCount = newCount;
    }
}

// It is Raised when a user leaves a session.
public class UserLeftEventArgs : SessionEventArgs
{
    public ControllerUser User { get; }
    public int NewCount { get; }

    public UserLeftEventArgs(ControllerUser user, int newCount)
    {
        User = user;
        NewCount = newCount;
    }
}

// It will be Raised when a session ends.
public class MeetingEndedEventArgs : SessionEventArgs
{
    public ControllerUser Host { get; }

    public MeetingEndedEventArgs(ControllerUser host)
    {
        Host=host;
    }
}
