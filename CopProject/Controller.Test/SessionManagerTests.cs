using Controller;
using Utils;
using Xunit;

namespace Controller.Test;

public class SessionManagerTests
{
    [Fact]
    public void CreateAndStartSession()
    {
        var manager = new SessionManager();
        var host1 = new ControllerUser("Host1", "host1@test.com", null, "10.10.10.10", 9990, isHost: true);
        var session = manager.CreateSession(host1);
        bool started = manager.StartSession(host1);
        Assert.NotNull(session);
        Assert.True(started);
        Assert.True(manager.HasActiveSession());
        Assert.Equal(1, manager.GetActiveUserCount());
        var host2 = new ControllerUser("Host2", "host2@test.com", null, "10.10.10.10", 9991, isHost: true);
        var duplicate = manager.CreateSession(host2);
        Assert.Null(duplicate);
    }


    [Fact]
    public void JoinAndLeaveSession()
    {
        var manager = new SessionManager();
        var host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990,isHost: true);
        manager.CreateSession(host);
        manager.StartSession(host);
        var client = new ControllerUser("ClientUser", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        bool joined = manager.JoinSession(client);
        Assert.True(joined);
        Assert.Equal(2, manager.GetActiveUserCount());
        Assert.Equal(2, manager.GetActiveUsers().Count);
        bool left = manager.LeaveSession(client.UserKey);
        Assert.True(left);
        Assert.Equal(1, manager.GetActiveUserCount());
    }

    [Fact]
    public void EndSession()
    {
        var manager = new SessionManager();
        var host = new ControllerUser("HostUser", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        manager.CreateSession(host);
        manager.StartSession(host);
        bool ended = manager.EndSession(host.UserKey);
        Assert.True(ended);
        Assert.False(manager.HasActiveSession());
    }

    [Fact]
    public void OperationsWithoutActiveSession()
    {
        var manager = new SessionManager();
        var dummy = new ControllerUser("Dummy", "dummy@test.com", null, "10.10.10.10", 9990, isHost: false);
        Assert.False(manager.StartSession(dummy));
        Assert.False(manager.JoinSession(dummy));
        Assert.False(manager.LeaveSession("unknownKey"));
        Assert.False(manager.EndSession("unknownKey"));
        Assert.Equal(0, manager.GetActiveUserCount());
        Assert.Empty(manager.GetActiveUsers());
    }

    [Fact]
    public void SessionMessageListener_HandleJoinAndLeave()
    {
        var manager = new SessionManager();
        var host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        manager.CreateSession(host);
        manager.StartSession(host);
        var listener = new SessionMessageListener(manager);
        var client = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        string joinMsg = Protocol.FormatJoinRequest(client);
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes(joinMsg));
        Assert.Equal(2, manager.GetActiveUserCount());
        string leaveMsg = Protocol.FormatLeaveRequest(client.UserKey);
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes(leaveMsg));
        Assert.Equal(1, manager.GetActiveUserCount());
    }

    [Fact]
    public void SessionMessageListener_MalformedAndEmptyMessages()
    {
        var manager = new SessionManager();
        var listener = new SessionMessageListener(manager);
        Assert.Throws<ArgumentNullException>(() => new SessionMessageListener(null!));
        var client = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes(Protocol.FormatJoinRequest(client)));
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes(Protocol.FormatLeaveRequest(client.UserKey)));
        listener.OnMessageReceived(Array.Empty<byte>());
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes("NO_SEPARATOR"));
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes("UNKNOWN_TYPE|payload"));
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes("JOIN|not-valid-json"));
    }
}