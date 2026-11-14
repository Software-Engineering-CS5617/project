using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Packets;
using Networking.TCPCommunication;
using Utils;

namespace Networking.Test;

/// <summary>
/// Unit tests for CommunicatorServer and CommunicatorClient.
/// </summary>
[TestClass]
public class CommunicatorTests
{
    private sealed class TestMessageListener : IMessageListener
    {
        private readonly ManualResetEventSlim _signal = new(false);

        public byte[]? ReceivedData { get; private set; }

        public void OnMessageReceived(byte[] message)
        {
            ReceivedData = message;
            _signal.Set();
        }

        public bool WaitForMessage(int timeoutMilliseconds = 3000)
        {
            return _signal.Wait(timeoutMilliseconds);
        }

        public void Reset()
        {
            _signal.Reset();
            ReceivedData = null;
        }
    }

    private static int GetAvailablePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Checks that CommunicatorServer binds to the given port and loopback IP.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_Initialize_ListensOnConfiguredPort()
    {
        int port = GetAvailablePort();
        IPEndPoint endpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(endpoint);

        Assert.AreEqual(port, server.ListenPort);
        Assert.AreEqual(IPAddress.Loopback, server.ListenIpAddress);

        using TcpClient probe = new();
        probe.Connect(IPAddress.Loopback, port);
        Assert.IsTrue(probe.Connected);
    }

    /// <summary>
    /// Checks that a server created with a null endpoint finds an available port and IP.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_InitializeWithNull_AssignsValidIpAddress()
    {
        using CommunicatorServer server = new(null);

        Assert.IsNotNull(server.ListenIpAddress);
        Assert.AreNotEqual(IPAddress.Any, server.ListenIpAddress);
        Assert.IsGreaterThan(0, server.ListenPort);
    }

    /// <summary>
    /// Checks adding and removing a subscriber on CommunicatorServer.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_AddAndRemoveSubscriber_ExecutesWithoutError()
    {
        int port = GetAvailablePort();
        using CommunicatorServer server = new(new IPEndPoint(IPAddress.Loopback, port));
        TestMessageListener listener = new();

        server.AddSubscriber(10, listener);
        server.RemoveSubscriber(10);
    }

    /// <summary>
    /// Checks adding and removing a subscriber on CommunicatorClient.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_AddAndRemoveSubscriber_ExecutesWithoutError()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        using CommunicatorClient client = new(serverEndpoint);
        TestMessageListener listener = new();

        client.AddSubscriber(20, listener);
        client.RemoveSubscriber(20);
    }

    /// <summary>
    /// Checks that CommunicatorClient exposes a non-zero port and loopback IP when connected.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_ListenPortAndIp_ReturnsValidValuesWhenConnected()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        using CommunicatorClient client = new(serverEndpoint);

        Assert.IsGreaterThan(0, client.ListenPort);
        Assert.IsNotNull(client.ListenIpAddress);
        Assert.IsTrue(IPAddress.IsLoopback(client.ListenIpAddress));
    }

    /// <summary>
    /// Checks that client returns IPAddress.Any after disposal.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_ListenIpAddress_ReturnsAnyWhenDisposed()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        CommunicatorClient client = new(serverEndpoint);

        client.Dispose();

        Assert.AreEqual(IPAddress.Any, client.ListenIpAddress);
        Assert.AreEqual(0, client.ListenPort);
    }

    /// <summary>
    /// Checks sending a message from client to server.
    /// </summary>
    [TestMethod]
    public void SendMessage_ClientToServer_DeliversMessageToSubscriber()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        TestMessageListener serverListener = new();
        server.AddSubscriber(1, serverListener);

        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(150);

        byte[] payload = Encoding.UTF8.GetBytes("Client to Server Message");
        client.SendMessage(serverEndpoint, payload, 1, 1);

        Assert.IsTrue(serverListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, serverListener.ReceivedData);
    }

    /// <summary>
    /// Checks broadcasting a message from server to a connected client.
    /// </summary>
    [TestMethod]
    public void BroadcastMessage_ServerToClients_DeliversToSubscribedClient()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client = new(serverEndpoint);
        TestMessageListener clientListener = new();
        client.AddSubscriber(5, clientListener);
        Thread.Sleep(150);

        byte[] broadcastPayload = Encoding.UTF8.GetBytes("Server Broadcast Payload");
        server.BroadcastMessage(broadcastPayload, 5, 1);

        Assert.IsTrue(clientListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(broadcastPayload, clientListener.ReceivedData);
    }

    /// <summary>
    /// Checks that sending to an unregistered module does not throw.
    /// </summary>
    [TestMethod]
    public void SendMessage_UnregisteredModule_DoesNotThrow()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(150);

        byte[] payload = Encoding.UTF8.GetBytes("Unregistered Module Message");
        client.SendMessage(serverEndpoint, payload, 999, 1);

        Thread.Sleep(200);
    }

    /// <summary>
    /// Checks that all connected clients receive a broadcast message.
    /// </summary>
    [TestMethod]
    public void BroadcastMessage_MultipleClients_BothReceiveBroadcast()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        client1.AddSubscriber(8, listener1);
        client2.AddSubscriber(8, listener2);

        Thread.Sleep(200);

        byte[] broadcastData = Encoding.UTF8.GetBytes("Multiclient Broadcast");
        server.BroadcastMessage(broadcastData, 8, 1);

        Assert.IsTrue(listener1.WaitForMessage(3000));
        Assert.IsTrue(listener2.WaitForMessage(3000));
        CollectionAssert.AreEqual(broadcastData, listener1.ReceivedData);
        CollectionAssert.AreEqual(broadcastData, listener2.ReceivedData);
    }

    /// <summary>
    /// Checks CommunicatorServer multicast message delivery to group members.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_MulticastMessage_DeliversOnlyToSubscribedGroupMembers()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        client1.AddSubscriber(15, listener1);
        client2.AddSubscriber(15, listener2);

        Thread.Sleep(200);

        IPEndPoint client1Ep = new(client1.ListenIpAddress, client1.ListenPort);
        server.JoinMulticastGroup(101, client1Ep);

        byte[] payload = Encoding.UTF8.GetBytes("Multicast Message Payload");
        server.MulticastMessage(101, payload, 15, 1);

        Assert.IsTrue(listener1.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listener1.ReceivedData);
        Assert.IsFalse(listener2.WaitForMessage(500));

        listener1.Reset();
        IPEndPoint client2Ep = new(client2.ListenIpAddress, client2.ListenPort);
        server.JoinMulticastGroup(101, client2Ep);
        server.LeaveMulticastGroup(101, client1Ep);
        server.MulticastMessage(101, payload, 15, 1);
        Assert.IsFalse(listener1.WaitForMessage(500));

        server.LeaveMulticastGroup(101, client2Ep);
        server.LeaveMulticastGroup(999, client1Ep);

        IPEndPoint ipv6Mapped = new(IPAddress.Parse("::ffff:127.0.0.1"), 8888);
        server.JoinMulticastGroup(202, ipv6Mapped);
        server.LeaveMulticastGroup(202, ipv6Mapped);
        server.MulticastMessage(999, payload, 15, 1);
    }

    /// <summary>
    /// Checks CommunicatorServer client removal for existing and non-existing clients.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_RemoveClient_HandlesPresentAndMissingClients()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        IPEndPoint nonExisting = new(IPAddress.Loopback, 12345);
        server.RemoveClient(nonExisting);

        using TcpClient connectedTcp = new();
        connectedTcp.Connect(IPAddress.Loopback, port);
        IPEndPoint clientEp = (IPEndPoint)connectedTcp.Client.LocalEndPoint!;
        if (clientEp.Address.IsIPv4MappedToIPv6)
        {
            clientEp = new IPEndPoint(clientEp.Address.MapToIPv4(), clientEp.Port);
        }

        server.AddClient(clientEp, connectedTcp);
        server.JoinMulticastGroup(505, clientEp);
        server.RemoveClient(clientEp);
    }

    /// <summary>
    /// Checks that CommunicatorServer implements IUserNotificationHandler.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_ImplementsIUserNotificationHandler()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        Assert.IsInstanceOfType<IUserNotificationHandler>(server);
    }

    /// <summary>
    /// Checks that OnUserLeft cleans up client maps and multicast memberships.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_OnUserLeft_RemovesClientFromMapsAndMulticast()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using TcpClient connectedTcp = new();
        connectedTcp.Connect(IPAddress.Loopback, port);
        IPEndPoint clientEp = (IPEndPoint)connectedTcp.Client.LocalEndPoint!;
        if (clientEp.Address.IsIPv4MappedToIPv6)
        {
            clientEp = new IPEndPoint(clientEp.Address.MapToIPv4(), clientEp.Port);
        }

        server.AddClient(clientEp, connectedTcp);
        server.JoinMulticastGroup(404, clientEp);

        var tcpMap = (System.Collections.Concurrent.ConcurrentDictionary<IPEndPoint, TcpClient>?)
            typeof(CommunicatorServer).GetField("_clientToTcpListenerMap", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(server);
        var multicastMap = (System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<IPEndPoint, byte>>?)
            typeof(CommunicatorServer).GetField("_multicastGroupMap", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(server);

        Assert.IsNotNull(tcpMap);
        Assert.IsNotNull(multicastMap);
        Assert.IsTrue(tcpMap.ContainsKey(clientEp));
        Assert.IsTrue(multicastMap[404].ContainsKey(clientEp));

        ControllerUser user = new("Bob", "bob@example.com", null, clientEp.Address.ToString(), clientEp.Port);
        server.OnUserLeft(user);

        Assert.IsFalse(tcpMap.ContainsKey(clientEp));
        Assert.IsFalse(multicastMap[404].ContainsKey(clientEp));
    }

    /// <summary>
    /// Checks that OnUserJoined and OnUserLeft handle null or malformed inputs without throwing.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_UserNotifications_HandleNullAndInvalidGracefully()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        server.OnUserJoined(null!);
        server.OnUserJoined(new ControllerUser("Alice", "alice@example.com", null, "127.0.0.1", 1234));

        server.OnUserLeft(null!);
        server.OnUserLeft(new ControllerUser { Ip = "not-an-ip", Port = 5000 });
        server.OnUserLeft(new ControllerUser { Ip = "127.0.0.1", Port = 0 });
        server.OnUserLeft(new ControllerUser { Ip = "127.0.0.1", Port = -1 });
    }

    /// <summary>
    /// Checks CommunicatorServer initialization failure when port is already in use.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_PortAlreadyInUse_CatchesExceptionAndDisposesSafely()
    {
        int port = GetAvailablePort();
        using TcpListener blocker = new(IPAddress.Loopback, port);
        blocker.Start();

        CommunicatorServer failedServer = new(new IPEndPoint(IPAddress.Loopback, port));
        failedServer.AcceptClientConnections();

        Assert.AreEqual(0, failedServer.ListenPort);
        Assert.AreEqual(IPAddress.Any, failedServer.ListenIpAddress);

        failedServer.Dispose();
    }

    /// <summary>
    /// Checks that multiple Dispose calls on CommunicatorServer and CommunicatorClient are safe.
    /// </summary>
    [TestMethod]
    public void Communicator_MultipleDispose_DoesNotThrow()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        CommunicatorServer server = new(serverEndpoint);
        CommunicatorClient client = new(serverEndpoint);

        server.Dispose();
        server.Dispose();

        client.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Checks that AcceptClientConnections safely terminates without unhandled exception when server socket is disposed.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_AcceptClientConnections_HandlesDisposedSocketGracefully()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        CommunicatorServer server = new(serverEndpoint);

        server.Dispose();

        // Calling AcceptClientConnections on disposed server must not throw ObjectDisposedException
        server.AcceptClientConnections();
    }

    /// <summary>
    /// Checks CommunicatorClient initialization failure with unreachable endpoint.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_UnreachableServer_CatchesException()
    {
        IPEndPoint closedEndpoint = new(IPAddress.Loopback, 1);
        CommunicatorClient client = new(closedEndpoint);

        Assert.AreEqual(0, client.ListenPort);
        Assert.AreEqual(IPAddress.Any, client.ListenIpAddress);
        client.Dispose();

        Assert.AreEqual(0, client.ListenPort);
        Assert.AreEqual(IPAddress.Any, client.ListenIpAddress);
    }

    /// <summary>
    /// Checks that client broadcast delivers to all connected clients except the sender.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_BroadcastMessage_DeliversToOtherClientsExceptSender()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);
        using CommunicatorClient client3 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        TestMessageListener listener3 = new();

        client1.AddSubscriber(25, listener1);
        client2.AddSubscriber(25, listener2);
        client3.AddSubscriber(25, listener3);

        Thread.Sleep(200);

        byte[] payload = Encoding.UTF8.GetBytes("Client Broadcast Payload");
        client1.BroadcastMessage(payload, 25, 1);

        Assert.IsTrue(listener2.WaitForMessage(3000));
        Assert.IsTrue(listener3.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listener2.ReceivedData);
        CollectionAssert.AreEqual(payload, listener3.ReceivedData);
        Assert.IsFalse(listener1.WaitForMessage(500));
    }

    /// <summary>
    /// Checks that client multicast delivers to subscribed group members including sender.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_MulticastMessage_DeliversToGroupMembers()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);
        using CommunicatorClient client3 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        TestMessageListener listener3 = new();

        client1.AddSubscriber(26, listener1);
        client2.AddSubscriber(26, listener2);
        client3.AddSubscriber(26, listener3);

        Thread.Sleep(200);

        IPEndPoint client1Ep = new(client1.ListenIpAddress, client1.ListenPort);
        IPEndPoint client2Ep = new(client2.ListenIpAddress, client2.ListenPort);
        server.JoinMulticastGroup(300, client1Ep);
        server.JoinMulticastGroup(300, client2Ep);

        byte[] payload = Encoding.UTF8.GetBytes("Client Multicast Payload");
        client1.MulticastMessage(300, payload, 26, 1);

        Assert.IsTrue(listener1.WaitForMessage(3000));
        Assert.IsTrue(listener2.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listener1.ReceivedData);
        CollectionAssert.AreEqual(payload, listener2.ReceivedData);
        Assert.IsFalse(listener3.WaitForMessage(500));
    }

    /// <summary>
    /// Checks that client multicast to a non-existent group does not deliver any messages.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_MulticastMessage_NonExistentGroup_DoesNotDeliver()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        using CommunicatorClient client = new(serverEndpoint);

        TestMessageListener listener = new();
        client.AddSubscriber(26, listener);
        Thread.Sleep(150);

        byte[] payload = Encoding.UTF8.GetBytes("Empty Group Multicast");
        client.MulticastMessage(99999, payload, 26, 1);

        Assert.IsFalse(listener.WaitForMessage(500));
    }

    /// <summary>
    /// Checks that bidirectional broadcasts between two clients succeed and exclude sender each time.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_BidirectionalBroadcast_BothReceiveEachOthersMessages()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        client1.AddSubscriber(27, listener1);
        client2.AddSubscriber(27, listener2);

        Thread.Sleep(200);

        byte[] messageFrom1 = Encoding.UTF8.GetBytes("Message from Client 1");
        client1.BroadcastMessage(messageFrom1, 27, 1);

        Assert.IsTrue(listener2.WaitForMessage(3000));
        Assert.IsFalse(listener1.WaitForMessage(500));
        CollectionAssert.AreEqual(messageFrom1, listener2.ReceivedData);

        listener2.Reset();
        byte[] messageFrom2 = Encoding.UTF8.GetBytes("Message from Client 2");
        client2.BroadcastMessage(messageFrom2, 27, 1);

        Assert.IsTrue(listener1.WaitForMessage(3000));
        Assert.IsFalse(listener2.WaitForMessage(500));
        CollectionAssert.AreEqual(messageFrom2, listener1.ReceivedData);
    }

    /// <summary>
    /// Checks that server BroadcastMessage with excludeClient parameter skips the specified endpoint.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_BroadcastMessage_WithExcludeEndpoint_SkipsTarget()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        client1.AddSubscriber(28, listener1);
        client2.AddSubscriber(28, listener2);

        Thread.Sleep(200);

        IPEndPoint client1Ep = new(client1.ListenIpAddress, client1.ListenPort);
        byte[] payload = Encoding.UTF8.GetBytes("Server Broadcast With Exclude");
        server.BroadcastMessage(payload, 28, 1, client1Ep);

        Assert.IsTrue(listener2.WaitForMessage(3000));
        Assert.IsFalse(listener1.WaitForMessage(500));
        CollectionAssert.AreEqual(payload, listener2.ReceivedData);

        // Also check with IPv4-mapped IPv6 address format
        listener2.Reset();
        IPEndPoint ipv6Mapped = new(IPAddress.Parse("::ffff:" + client1.ListenIpAddress), client1.ListenPort);
        server.BroadcastMessage(payload, 28, 1, ipv6Mapped);
        Assert.IsTrue(listener2.WaitForMessage(3000));
        Assert.IsFalse(listener1.WaitForMessage(500));

        // Check with different IP same port
        listener1.Reset();
        listener2.Reset();
        IPEndPoint differentIpSamePort = new(IPAddress.Parse("127.0.0.2"), client1.ListenPort);
        server.BroadcastMessage(payload, 28, 1, differentIpSamePort);
        Assert.IsTrue(listener1.WaitForMessage(3000));
        Assert.IsTrue(listener2.WaitForMessage(3000));

        // Check with different port
        listener1.Reset();
        listener2.Reset();
        IPEndPoint differentPort = new(client1.ListenIpAddress, 1);
        server.BroadcastMessage(payload, 28, 1, differentPort);
        Assert.IsTrue(listener1.WaitForMessage(3000));
        Assert.IsTrue(listener2.WaitForMessage(3000));
    }

    /// <summary>
    /// Checks that server MulticastMessage with excludeClient parameter skips the specified endpoint.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_MulticastMessage_WithExcludeEndpoint_SkipsTarget()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);

        TestMessageListener listener1 = new();
        TestMessageListener listener2 = new();
        client1.AddSubscriber(29, listener1);
        client2.AddSubscriber(29, listener2);

        Thread.Sleep(200);

        IPEndPoint client1Ep = new(client1.ListenIpAddress, client1.ListenPort);
        IPEndPoint client2Ep = new(client2.ListenIpAddress, client2.ListenPort);
        server.JoinMulticastGroup(400, client1Ep);
        server.JoinMulticastGroup(400, client2Ep);

        byte[] payload = Encoding.UTF8.GetBytes("Server Multicast With Exclude");
        server.MulticastMessage(400, payload, 29, 1, client1Ep);

        Assert.IsTrue(listener2.WaitForMessage(3000));
        Assert.IsFalse(listener1.WaitForMessage(500));
        CollectionAssert.AreEqual(payload, listener2.ReceivedData);

        // Also check with IPv4-mapped IPv6 address format
        listener2.Reset();
        IPEndPoint ipv6Mapped = new(IPAddress.Parse("::ffff:" + client1.ListenIpAddress), client1.ListenPort);
        server.MulticastMessage(400, payload, 29, 1, ipv6Mapped);
        Assert.IsTrue(listener2.WaitForMessage(3000));
        Assert.IsFalse(listener1.WaitForMessage(500));
    }

    /// <summary>
    /// Checks that CommunicatorClient broadcast and multicast handle disconnected sockets safely.
    /// </summary>
    [TestMethod]
    public void CommunicatorClient_BroadcastAndMulticast_Disconnected_DoesNotThrow()
    {
        IPEndPoint closedEndpoint = new(IPAddress.Loopback, 1);
        using CommunicatorClient client = new(closedEndpoint);

        byte[] payload = Encoding.UTF8.GetBytes("Disconnected Payload");
        client.BroadcastMessage(payload, 30, 1);
        client.MulticastMessage(100, payload, 30, 1);
    }

    /// <summary>
    /// Checks CommunicatorServer.NetworkMessageListener direct handling including broadcast, multicast, unknown and error cases.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_NetworkMessageListener_DirectInvocation()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        CommunicatorServer.NetworkMessageListener listener = new(server);

        // Safe default on invalid data
        listener.OnMessageReceived(Array.Empty<byte>());
        listener.OnMessageReceived(new byte[] { 0x01, 0x02 });

        Serializer<NetworkMessage> serializer = new();

        // Valid broadcast packet
        BroadcastPacket broadcast = new(Encoding.UTF8.GetBytes("Listener Broadcast Test"), 31, 1, new IPEndPoint(IPAddress.Loopback, 5000));
        byte[] broadcastBytes = serializer.Serialize(broadcast);
        listener.OnMessageReceived(broadcastBytes);

        // Valid multicast packet
        MulticastPacket multicast = new(50, Encoding.UTF8.GetBytes("Listener Multicast Test"), 32, 1, new IPEndPoint(IPAddress.Loopback, 5000));
        byte[] multicastBytes = serializer.Serialize(multicast);
        listener.OnMessageReceived(multicastBytes);
    }
}
