using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Controller;
using Controller.Models;
using Utils;
using Xunit;

namespace Controller.Test;

public class ClientControllerTests
{
    [Fact]
    public void StartAndEndMeeting()
    {
        var clientUser = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        var client = (ClientController)ControllerFactory.CreateClient(clientUser);
        Assert.False(client.StartMeeting());
        Assert.False(client.EndMeeting());
        Assert.False(client.IsConnected);
        Assert.Equal(0, client.ListenPort);
        client.LeaveMeeting(clientUser);
        client.Dispose();
    }

    [Fact]
    public void JoinMeetingWithInvalidIp()
    {
        var clientUser = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        var client = (ClientController)ControllerFactory.CreateClient(clientUser);
        bool joined = client.JoinMeeting("invalid", 5000, clientUser);
        Assert.False(joined);
        client.Dispose();
    }
    

    [Fact]
    public void NotificationHandler_UserJoined()
    {
        var clientUser = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        var client = (ClientController)ControllerFactory.CreateClient(clientUser);
        var handler = new TestNotificationHandler();
        client.RegisterNotificationHandler(handler);
        client.NotifyHandlersUserJoined(clientUser);
        Assert.NotNull(handler.LastJoinedUser);
        Assert.Equal("Client", handler.LastJoinedUser.Username);
        var peer = new ControllerUser("Peer", "peer@test.com", null, "10.10.10.10", 9992, isHost: false);
        client.NotifyHandlersUserJoined(peer);
        Assert.Equal("Peer", handler.LastJoinedUser.Username);
        client.Dispose();
    }

    [Fact]
    public void NotificationHandler_UserLeft()
    {
        var clientUser = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        var client = (ClientController)ControllerFactory.CreateClient(clientUser);
        var handler = new TestNotificationHandler();
        client.RegisterNotificationHandler(handler);
        var peer = new ControllerUser("Peer", "peer@test.com", null, "10.10.10.10", 9992, isHost: false);
        client.NotifyHandlersUserLeft(peer);
        Assert.Equal("Peer", handler.LastLeftUser?.Username);
        client.Dispose();
    }

    [Fact]
    public void SyncUserList_And_RaiseEvents()
    {
        var clientUser = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        var client = (ClientController)ControllerFactory.CreateClient(clientUser);
        var peer = new ControllerUser("Peer", "peer@test.com", null, "10.10.10.10", 9992, isHost: false);
        client.SyncUserList(new List<ControllerUser> { clientUser, peer });
        client.UpdateUserCount(2);
        Assert.Equal(2, client.GetActiveUserCount());
        Assert.Equal(2, client.GetActiveUsers().Count);
        Assert.Equal(2, client.GetLastKnownUserCount());
        bool joinedFired = false;
        bool leftFired = false;
        bool endedFired = false;
        client.UserJoined += (s, e) => joinedFired = true;
        client.UserLeft += (s, e) => leftFired = true;
        client.MeetingEnded += (s, e) => endedFired = true;
        client.RaiseUserJoined(new UserJoinedEventArgs(clientUser, 1));
        client.RaiseUserLeft(new UserLeftEventArgs(clientUser, 0));
        client.RaiseMeetingEnded(new MeetingEndedEventArgs(clientUser));
        Assert.True(joinedFired);
        Assert.True(leftFired);
        Assert.True(endedFired);
        client.Dispose();
    }

    [Fact]
    public void Client_NotificationHandlers_RegisterAndUnregister()
    {
        var clientUser = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        var client = (ClientController)ControllerFactory.CreateClient(clientUser);
        var handler = new TestNotificationHandler();

        client.RegisterNotificationHandler(handler);
        client.UnregisterNotificationHandler(handler);
        client.RegisterNotificationHandler(null!);
        client.UnregisterNotificationHandler(null!);

        client.Dispose();
    }
    

    [Fact]
    public void CreateClientWithFactory()
    {
        var client = (ClientController)ControllerFactory.CreateClient("Sai", "saideep@smail.iitpkd.ac.in");
        Assert.NotNull(client.CurrentUser);
        Assert.Equal("Sai", client.CurrentUser.Username);
        Assert.False(client.CurrentUser.IsHost);
        client.Dispose();
    }

    [Fact]
    public void MeetingService_CreateAndManageSession()
    {
        var service = new ControllerMeetingService();
        var session = new MeetingSession { CurrentUserId = "Sai", CurrentUserEmail = "saideep@smail.iitpkd.ac.in" };
        service.Initialize(session);
        var handler = new TestNotificationHandler();
        service.RegisterNotificationHandler(handler);
        var (id, port) = service.CreateMeeting();
        Assert.False(string.IsNullOrEmpty(id));
        Assert.False(string.IsNullOrEmpty(port));
        Assert.NotNull(service.ActiveController);
        service.UnregisterNotificationHandler(handler);
        session.EndMeeting();
    }

    [Fact]
    public void ClientNotificationListener_HandleMessages()
    {
        var clientUser = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        var client = (ClientController)ControllerFactory.CreateClient(clientUser);
        var handler = new TestNotificationHandler();
        client.RegisterNotificationHandler(handler);
        var listener = new ClientNotificationListener(client);
        var peer1 = new ControllerUser("Peer1", "peer1@test.com", null, "10.10.10.10", 9992, isHost: false);
        string payload1 = $"{JsonSerializer.Serialize(peer1)}|{JsonSerializer.Serialize(new List<ControllerUser> { clientUser, peer1 })}";
        string joinMsg1 = Protocol.FormatNotification(Protocol.NotifyUserJoined, payload1);
        listener.OnMessageReceived(Encoding.UTF8.GetBytes(joinMsg1));
        Assert.Equal(2, client.GetActiveUserCount());
        var peer2 = new ControllerUser("Peer2", "peer2@test.com", null, "10.10.10.10", 9993, isHost: false);
        string payload2 = $"{JsonSerializer.Serialize(peer2)}|{JsonSerializer.Serialize(new List<ControllerUser> { clientUser, peer1, peer2 })}";
        string joinMsg2 = Protocol.FormatNotification(Protocol.NotifyUserJoined, payload2);
        listener.OnMessageReceived(Encoding.UTF8.GetBytes(joinMsg2));
        Assert.Equal(3, client.GetActiveUserCount());
        string leavePayload = $"{JsonSerializer.Serialize(peer2)}|{JsonSerializer.Serialize(new List<ControllerUser> { clientUser, peer1 })}";
        string leaveMsg = Protocol.FormatNotification(Protocol.NotifyUserLeft, leavePayload);
        listener.OnMessageReceived(Encoding.UTF8.GetBytes(leaveMsg));
        Assert.Equal(2, client.GetActiveUserCount());
        string endMsg = Protocol.FormatNotification(Protocol.NotifySessionEnded, JsonSerializer.Serialize(clientUser));
        listener.OnMessageReceived(Encoding.UTF8.GetBytes(endMsg));
        Assert.Equal(0, client.GetActiveUserCount());
        listener.OnMessageReceived(Array.Empty<byte>());
        listener.OnMessageReceived(Encoding.UTF8.GetBytes("INVALID_FORMAT"));
        listener.OnMessageReceived(Encoding.UTF8.GetBytes("UNKNOWN_TYPE|payload"));
        listener.OnMessageReceived(Encoding.UTF8.GetBytes(Protocol.NotifyUserJoined + "|not-json"));
        listener.OnMessageReceived(Encoding.UTF8.GetBytes(Protocol.NotifyUserLeft + "|not-json"));
        listener.OnMessageReceived(Encoding.UTF8.GetBytes(Protocol.NotifySessionEnded + "|not-json"));
        Assert.Throws<ArgumentNullException>(() => new ClientController(null!));
        client.Dispose();
        client.Dispose();
    }
   
}