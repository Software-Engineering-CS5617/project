using Controller;
using Networking;
using Utils;

namespace Screenshare.Tests.Fakes;

/// <summary>
/// stands in for the Controller when testing ControllerMeetingSession: it only hands out the
/// current user and connection, and records who registered for notifications.
/// </summary>
internal sealed class FakeControllerService : IControllerService
{
    public ControllerUser? CurrentUser { get; set; }

    public ICommunicator? Communicator { get; set; }

    /// <summary>handlers currently registered</summary>
    public List<IUserNotificationHandler> Handlers { get; } = new();

    /// <summary>how many times UnregisterNotificationHandler was called</summary>
    public int UnregisterCalls { get; private set; }

    /// <summary>when true, unregistering throws, like a controller that was already disposed</summary>
    public bool FailUnregister { get; set; }

    public void RegisterNotificationHandler(IUserNotificationHandler handler)
    {
        Handlers.Add(handler);
    }

    public void UnregisterNotificationHandler(IUserNotificationHandler handler)
    {
        UnregisterCalls++;
        if (FailUnregister)
        {
            throw new ObjectDisposedException(nameof(FakeControllerService));
        }

        Handlers.Remove(handler);
    }

    // the rest of the Controller is not used by ScreenShare

#pragma warning disable CS0067 // events are never raised
    public event EventHandler<UserJoinedEventArgs>? UserJoined;
    public event EventHandler<UserLeftEventArgs>? UserLeft;
    public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;
#pragma warning restore CS0067

    public int ListenPort => Communicator?.ListenPort ?? 0;

    public bool StartMeeting() => throw new NotSupportedException();

    public bool EndMeeting() => throw new NotSupportedException();

    public bool JoinMeeting(string hostIp, int hostPort, ControllerUser user) => throw new NotSupportedException();

    public void LeaveMeeting(ControllerUser user) => throw new NotSupportedException();

    public int GetActiveUserCount() => throw new NotSupportedException();

    public List<ControllerUser> GetActiveUsers() => throw new NotSupportedException();

    public void Dispose()
    {
    }
}
