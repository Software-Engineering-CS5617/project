namespace Controller;

// The single interface the UX team programs against.
//   The UX layer depends on this abstraction, not on concrete Host/Client classes.
//   It exposes commands (StartMeeting, EndMeeting, JoinMeeting, LeaveMeeting) and events so the UI updates automatically.
// Each session is uniquely identified by the host's IP:port pair,

public interface IControllerService : IDisposable
{
    // Events
    // Fired when a meeting session starts 
    event EventHandler<SessionStartedEventArgs>? SessionStarted;
    //Fired when a meeting session ends.
    event EventHandler<SessionEndedEventArgs>? SessionEnded;
    // Fired when a user joins a session.
    event EventHandler<UserJoinedEventArgs>? UserJoined;
    // Fired when a user leaves a session.
    event EventHandler<UserLeftEventArgs>? UserLeft;
    //Fired when the active participant count changes.
    event EventHandler<CountChangedEventArgs>? CountChanged;

    
    // returns True if the meeting was created and started successfully.
    bool StartMeeting();

    // returns True if the meeting was ended successfully.
    bool EndMeeting();

    // Joins an existing meeting on a remote host.
    
    void JoinMeeting(string hostIp, int hostPort, string userId);

    // Leaves a meeting that was previously joined.
   
    void LeaveMeeting(string userId);

    // Gets the number of active participants in the session.
    int GetActiveUserCount();

    // Gets the user IDs of all active participants in the session.
    List<string> GetActiveUsers();

    // Gets the port this controller is listening on.
    int ListenPort { get; }
}
