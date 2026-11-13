using Controller;
using Utils;
using Xunit;

namespace Controller.Test;

class TestNotificationHandler : IUserNotificationHandler
{
    public ControllerUser? LastJoinedUser;
    public ControllerUser? LastLeftUser;

    public void OnUserJoined(ControllerUser user) => LastJoinedUser = user;
    public void OnUserLeft(ControllerUser user) => LastLeftUser = user;
}

public class HostControllerTests
{
    [Fact]
    public void StartMeeting()
    {
        var Host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var host = (HostController)ControllerFactory.CreateHost(Host);
        var handler = new TestNotificationHandler();
        host.RegisterNotificationHandler(handler);
        bool started = host.StartMeeting();
        Assert.True(started);
        Assert.True(host.IsMeetingActive);
        Assert.Equal(1, host.GetActiveUserCount());
        Assert.NotNull(handler.LastJoinedUser);
        Assert.Equal("Host", handler.LastJoinedUser.Username);
        host.Dispose();
    }

    [Fact]
    public void StartAlreadyStartedMeeting()
    {
        var Host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var host = (HostController)ControllerFactory.CreateHost(Host);
        host.StartMeeting();
        bool secondStart = host.StartMeeting();
        Assert.False(secondStart);
        host.Dispose();
    }

    [Fact]
    public void EndMeeting()
    {
        var Host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var host = (HostController)ControllerFactory.CreateHost(Host);
        host.StartMeeting();
        bool ended = host.EndMeeting();
        Assert.True(ended);
        Assert.False(host.IsMeetingActive);
        host.Dispose();
    }

    [Fact]
    public void EndMeetingWithoutAnActiveMeeting()
    {
        var Host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var host = (HostController)ControllerFactory.CreateHost(Host);
        bool ended = host.EndMeeting();
        Assert.False(ended);
        host.Dispose();
    }

    [Fact]
    public void JoinAndLeaveMeeting()
    {
        var Host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var host = (HostController)ControllerFactory.CreateHost(Host);
        var dummy = new ControllerUser("Dummy", "dummy@test.com", null, "10.10.10.10", 9991, isHost: false);
        Assert.False(host.JoinMeeting("10.10.10.10", 9991, dummy));
        host.LeaveMeeting(dummy);
        host.Dispose();
    }

    [Fact]
    public void NotificationHandlers()
    {
        var Host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var host = (HostController)ControllerFactory.CreateHost(Host);
        var handler = new TestNotificationHandler();
        host.RegisterNotificationHandler(handler);
        host.UnregisterNotificationHandler(handler);
        host.RegisterNotificationHandler(null!);
        host.UnregisterNotificationHandler(null!);
        host.Dispose();
    }

    [Fact]
    public void CreateWithFactory()
    {
        var host = (HostController)ControllerFactory.CreateHost("Sai", "saideep@smail.iitpkd.ac.in");
        Assert.NotNull(host.CurrentUser);
        Assert.Equal("Sai", host.CurrentUser.Username);
        Assert.True(host.CurrentUser.IsHost);
        host.Dispose();
    }
    [Fact]
    public void ParticipantJoinedAndLeft()
    {
        var Host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var host = (HostController)ControllerFactory.CreateHost(Host);
        var handler = new TestNotificationHandler();
        host.RegisterNotificationHandler(handler);
        host.StartMeeting();
        var client = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        host.SessionManager.JoinSession(client);
        Assert.Equal(2, host.GetActiveUserCount());
        Assert.Equal(2, host.GetActiveUsers().Count);
        Assert.Equal("Client", handler.LastJoinedUser?.Username);
        host.SessionManager.LeaveSession(client.UserKey);
        Assert.Equal(1, host.GetActiveUserCount());
        Assert.Equal("Client", handler.LastLeftUser?.Username);
        Assert.NotNull(host.Communicator);
        Assert.True(host.ListenPort > 0);
        Assert.NotNull(host.SessionManager);
        host.Dispose();
        host.Dispose(); 
    }
}