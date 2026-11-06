using Utils;
namespace Controller;

// The UX layer depends on this abstraction, not on concrete Host/Client classes.
// Each session is uniquely identified by the host's IP:port pair.

public interface IControllerService : IDisposable
{
    // Event Fired when a user joins a session.
    event EventHandler<UserJoinedEventArgs>? UserJoined;
    // Event Fired when a user leaves a session.
    event EventHandler<UserLeftEventArgs>? UserLeft;
    // Event Fired when the Meeting is Ended. 
    event EventHandler<MeetingEndedEventArgs>? MeetingEnded;
    // returns True if the meeting was created and started successfully.
    bool StartMeeting();

    // returns True if the meeting was ended successfully.
    bool EndMeeting();

    // Joins an existing meeting on a remote host.
    bool JoinMeeting(string hostIp, int hostPort, ControllerUser user);

    // Leaves a meeting that was previously joined.
    void LeaveMeeting(ControllerUser user);

    // Gets the number of active participants in the session.
    int GetActiveUserCount();

    // Gets the user information of all active participants in the session.
    List<ControllerUser> GetActiveUsers();

    // Gets the port this controller is listening on.
    int ListenPort { get; }
}
