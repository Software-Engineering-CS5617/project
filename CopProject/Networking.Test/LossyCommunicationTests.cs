using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Packets;
using Networking.Queues;
using Networking.Sockets;
using Networking.TCPCommunication;

namespace Networking.Test;

[TestClass]
public class LossyCommunicationTests
{
    private sealed class TestLossyMessageListener : IMessageListenerLossy
    {
        private readonly ManualResetEventSlim _signal = new(false);

        public byte[]? ReceivedData { get; private set; }

        public void OnMessageReceivedLossy(byte[] message)
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




    [TestMethod]
    public void SendMessageLossy_ClientToServer_DeliversMessageToLossySubscriber()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        TestLossyMessageListener serverListener = new();
        server.AddLossySubscriber(10, serverListener);

        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(200);

        byte[] payload = Encoding.UTF8.GetBytes("Client to Server Lossy Message");
        client.SendMessageLossy(serverEndpoint, payload, 10, 1);

        Assert.IsTrue(serverListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, serverListener.ReceivedData);
    }

    [TestMethod]
    public void SendMessageLossy_ServerToClient_DeliversMessageToLossySubscriber()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client = new(serverEndpoint);
        TestLossyMessageListener clientListener = new();
        client.AddLossySubscriber(20, clientListener);
        Thread.Sleep(200);

        IPEndPoint clientEndpoint = new(client.ListenIpAddress, client.ListenPort);
        byte[] payload = Encoding.UTF8.GetBytes("Server to Client Lossy Message");
        server.SendMessageLossy(clientEndpoint, payload, 20, 1);

        Assert.IsTrue(clientListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, clientListener.ReceivedData);
    }

    [TestMethod]
    public void LossySubscriber_RemoveSubscriber_DoesNotReceiveMessage()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        TestLossyMessageListener serverListener = new();
        server.AddLossySubscriber(30, serverListener);
        server.RemoveLossySubscriber(30);

        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(200);

        byte[] payload = Encoding.UTF8.GetBytes("Lossy message for removed subscriber");
        client.SendMessageLossy(serverEndpoint, payload, 30, 1);

        Assert.IsFalse(serverListener.WaitForMessage(500));
        Assert.IsNull(serverListener.ReceivedData);
    }

    [TestMethod]
    public void SendMessageLossy_UnregisteredModule_DoesNotThrow()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(200);

        byte[] payload = Encoding.UTF8.GetBytes("Unregistered module payload");
        client.SendMessageLossy(serverEndpoint, payload, 999, 1);
        Thread.Sleep(100);
    }

    [TestMethod]
    public void SendMessageLossy_ScreenshareTilePayload_TransmitsSuccessfully()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        TestLossyMessageListener serverListener = new();
        server.AddLossySubscriber(40, serverListener);

        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(200);

        // Simulate a screenshare tile of 8 KB
        byte[] tileData = new byte[8192];
        Random.Shared.NextBytes(tileData);

        client.SendMessageLossy(serverEndpoint, tileData, 40, 2);

        Assert.IsTrue(serverListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(tileData, serverListener.ReceivedData);
    }

    [TestMethod]
    public void BroadcastMessageLossy_ServerToConnectedClients_DeliversToSubscribedClient()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client = new(serverEndpoint);
        TestLossyMessageListener clientListener = new();
        client.AddLossySubscriber(50, clientListener);
        Thread.Sleep(200);

        byte[] payload = Encoding.UTF8.GetBytes("Server Lossy Broadcast");
        server.BroadcastMessageLossy(payload, 50, 1);

        Assert.IsTrue(clientListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, clientListener.ReceivedData);
    }

    [TestMethod]
    public void BroadcastMessageLossy_ClientDirectBroadcast_DeliversToOtherClientsAndServer()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        TestLossyMessageListener serverListener = new();
        server.AddLossySubscriber(55, serverListener);

        using CommunicatorClient clientA = new(serverEndpoint);
        TestLossyMessageListener listenerA = new();
        clientA.AddLossySubscriber(55, listenerA);

        using CommunicatorClient clientB = new(serverEndpoint);
        TestLossyMessageListener listenerB = new();
        clientB.AddLossySubscriber(55, listenerB);
        Thread.Sleep(250);

        byte[] payload = Encoding.UTF8.GetBytes("Client A Direct Lossy Broadcast");
        clientA.BroadcastMessageLossy(payload, 55, 1);

        // Client B receives the direct broadcast
        Assert.IsTrue(listenerB.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listenerB.ReceivedData);

        // Server also receives the direct broadcast
        Assert.IsTrue(serverListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, serverListener.ReceivedData);

        // Client A (the sender) receives loopback
        Assert.IsTrue(listenerA.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listenerA.ReceivedData);
    }

    [TestMethod]
    public void MulticastMessageLossy_ServerToGroupMembers_DeliversOnlyToSubscribedMembers()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient clientA = new(serverEndpoint);
        TestLossyMessageListener listenerA = new();
        clientA.AddLossySubscriber(60, listenerA);
        clientA.JoinMulticastGroup(88, new IPEndPoint(clientA.ListenIpAddress, clientA.ListenPort));

        using CommunicatorClient clientB = new(serverEndpoint);
        TestLossyMessageListener listenerB = new();
        clientB.AddLossySubscriber(60, listenerB);
        // Client B does NOT join group 88
        Thread.Sleep(250);

        byte[] payload = Encoding.UTF8.GetBytes("Multicast Group 88 Payload");
        server.MulticastMessageLossy(88, payload, 60, 1);

        Assert.IsTrue(listenerA.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listenerA.ReceivedData);

        Assert.IsFalse(listenerB.WaitForMessage(500));
        Assert.IsNull(listenerB.ReceivedData);
    }

    [TestMethod]
    public void MulticastMessageLossy_ClientDirectMulticast_DeliversOnlyToSubscribedMembers()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient clientA = new(serverEndpoint);

        using CommunicatorClient clientB = new(serverEndpoint);
        TestLossyMessageListener listenerB = new();
        clientB.AddLossySubscriber(65, listenerB);
        clientB.JoinMulticastGroup(77, new IPEndPoint(clientB.ListenIpAddress, clientB.ListenPort));

        using CommunicatorClient clientC = new(serverEndpoint);
        TestLossyMessageListener listenerC = new();
        clientC.AddLossySubscriber(65, listenerC);
        // Client C does NOT join group 77
        Thread.Sleep(250);

        byte[] payload = Encoding.UTF8.GetBytes("Client A Direct Multicast to Group 77");
        clientA.MulticastMessageLossy(77, payload, 65, 1);

        Assert.IsTrue(listenerB.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listenerB.ReceivedData);

        Assert.IsFalse(listenerC.WaitForMessage(500));
        Assert.IsNull(listenerC.ReceivedData);
    }

    [TestMethod]
    public void MulticastMessageLossy_ClientDirectMulticast_DeliversToServerIfJoined()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);
        TestLossyMessageListener serverListener = new();
        server.AddLossySubscriber(66, serverListener);
        server.JoinMulticastGroup(99, serverEndpoint);

        using CommunicatorClient client = new(serverEndpoint);
        Thread.Sleep(250);

        byte[] payload = Encoding.UTF8.GetBytes("Client to Server Direct Multicast");
        client.MulticastMessageLossy(99, payload, 66, 1);

        Assert.IsTrue(serverListener.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, serverListener.ReceivedData);
    }

    [TestMethod]
    public void LeaveMulticastGroup_StopsReceivingMulticast()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client = new(serverEndpoint);
        TestLossyMessageListener clientListener = new();
        client.AddLossySubscriber(80, clientListener);
        IPEndPoint clientEp = new(client.ListenIpAddress, client.ListenPort);

        client.JoinMulticastGroup(101, clientEp);
        Thread.Sleep(350);
        client.LeaveMulticastGroup(101, clientEp);
        Thread.Sleep(350);

        byte[] payload = Encoding.UTF8.GetBytes("Message after leaving group");
        server.MulticastMessageLossy(101, payload, 80, 1);

        Assert.IsFalse(clientListener.WaitForMessage(500));
        Assert.IsNull(clientListener.ReceivedData);
    }

    private sealed class DualProtocolListener : IMessageListener, IMessageListenerLossy
    {
        public readonly ManualResetEventSlim ReliableSignal = new(false);
        public readonly ManualResetEventSlim LossySignal = new(false);
        public byte[]? ReliableData { get; private set; }
        public byte[]? LossyData { get; private set; }

        public void OnMessageReceived(byte[] message)
        {
            ReliableData = message;
            ReliableSignal.Set();
        }

        public void OnMessageReceivedLossy(byte[] message)
        {
            LossyData = message;
            LossySignal.Set();
        }
    }

    [TestMethod]
    public void DualListener_ImplementsBothInterfaces_ReceivesReliableAndLossyIndependently()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client = new(serverEndpoint);
        DualProtocolListener dualListener = new();
        client.AddSubscriber(90, dualListener);
        client.AddLossySubscriber(90, dualListener);
        Thread.Sleep(200);

        byte[] reliablePayload = Encoding.UTF8.GetBytes("Reliable TCP Control");
        byte[] lossyPayload = Encoding.UTF8.GetBytes("Lossy UDP Frame");

        IPEndPoint clientEndpoint = new(client.ListenIpAddress, client.ListenPort);
        server.SendMessage(clientEndpoint, reliablePayload, 90, 1);
        server.SendMessageLossy(clientEndpoint, lossyPayload, 90, 1);

        Assert.IsTrue(dualListener.ReliableSignal.Wait(3000));
        Assert.IsTrue(dualListener.LossySignal.Wait(3000));
        CollectionAssert.AreEqual(reliablePayload, dualListener.ReliableData);
        CollectionAssert.AreEqual(lossyPayload, dualListener.LossyData);
    }

    [TestMethod]
    [DataRow(0, 239, 255, 0, 1)]
    [DataRow(1, 239, 255, 0, 1)]
    [DataRow(256, 239, 255, 1, 0)]
    [DataRow(65535, 239, 255, 255, 255)]
    public void GetMulticastAddress_MapsGroupIdToValidClassDMulticastAddress(int groupId, int b1, int b2, int b3, int b4)
    {
        IPAddress mapped = CommunicatorServer.GetMulticastAddress(groupId);
        byte[] bytes = mapped.GetAddressBytes();
        Assert.HasCount(4, bytes);
        Assert.AreEqual((byte)b1, bytes[0]);
        Assert.AreEqual((byte)b2, bytes[1]);
        Assert.AreEqual((byte)b3, bytes[2]);
        Assert.AreEqual((byte)b4, bytes[3]);
        Assert.IsTrue(bytes[0] >= 224 && bytes[0] <= 239);
    }

    [TestMethod]
    public void NativeMulticast_DifferentGroups_NoCrossTalk()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient clientGroupA = new(serverEndpoint);
        TestLossyMessageListener listenerA = new();
        clientGroupA.AddLossySubscriber(70, listenerA);
        clientGroupA.JoinMulticastGroup(10, new IPEndPoint(clientGroupA.ListenIpAddress, clientGroupA.ListenPort));

        using CommunicatorClient clientGroupB = new(serverEndpoint);
        TestLossyMessageListener listenerB = new();
        clientGroupB.AddLossySubscriber(70, listenerB);
        clientGroupB.JoinMulticastGroup(20, new IPEndPoint(clientGroupB.ListenIpAddress, clientGroupB.ListenPort));
        Thread.Sleep(250);

        byte[] payloadA = Encoding.UTF8.GetBytes("Group 10 Payload Only");
        server.MulticastMessageLossy(10, payloadA, 70, 1);

        Assert.IsTrue(listenerA.WaitForMessage(3000));
        CollectionAssert.AreEqual(payloadA, listenerA.ReceivedData);

        Assert.IsFalse(listenerB.WaitForMessage(500));
        Assert.IsNull(listenerB.ReceivedData);
    }

    [TestMethod]
    public void NativeBroadcast_MultipleClientsOnSharedPort_BothReceivePacket()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient client1 = new(serverEndpoint);
        using CommunicatorClient client2 = new(serverEndpoint);

        TestLossyMessageListener listener1 = new();
        TestLossyMessageListener listener2 = new();
        client1.AddLossySubscriber(75, listener1);
        client2.AddLossySubscriber(75, listener2);
        Thread.Sleep(250);

        byte[] broadcastData = Encoding.UTF8.GetBytes("Native Single-Packet Broadcast");
        server.BroadcastMessageLossy(broadcastData, 75, 1);

        Assert.IsTrue(listener1.WaitForMessage(3000));
        Assert.IsTrue(listener2.WaitForMessage(3000));
        CollectionAssert.AreEqual(broadcastData, listener1.ReceivedData);
        CollectionAssert.AreEqual(broadcastData, listener2.ReceivedData);
    }

    private sealed class ThrowingLossyListener : IMessageListenerLossy
    {
        public void OnMessageReceivedLossy(byte[] message)
        {
            throw new InvalidOperationException("Simulated lossy listener failure");
        }
    }

    /// <summary>
    /// Checks ReceivingQueueListenerLossy handling when subscriber throws or module is missing.
    /// </summary>
    [TestMethod]
    public void ReceivingQueueListenerLossy_UnhandledModuleAndThrowingSubscriber_Covered()
    {
        ReceivingQueue queue = new();
        ConcurrentDictionary<int, IMessageListenerLossy> map = new();
        map[50] = new ThrowingLossyListener();

        using ReceivingQueueListenerLossy listener = new(queue, map);

        Packet throwingPacket = new(new byte[] { 1, 2 }, new IPEndPoint(IPAddress.Loopback, 1000), 50, 1);
        queue.Enqueue(throwingPacket);

        Packet missingModulePacket = new(new byte[] { 3, 4 }, new IPEndPoint(IPAddress.Loopback, 1000), 9999, 1);
        queue.Enqueue(missingModulePacket);

        Thread.Sleep(200);
    }

    /// <summary>
    /// Checks SendingQueueListenerLossy handling when enqueued packet has null payload.
    /// </summary>
    [TestMethod]
    public void SendingQueueListenerLossy_InvalidSend_HandledGracefully()
    {
        using UdpClient udp = new(new IPEndPoint(IPAddress.Loopback, 0));
        SendingQueue queue = new();

        using SendingQueueListenerLossy listener = new(queue, udp);
        Packet invalidPacket = new(null!, new IPEndPoint(IPAddress.Loopback, GetAvailablePort()), 1, 1);
        queue.Enqueue(invalidPacket);
        Thread.Sleep(150);
    }

    /// <summary>
    /// Checks SendingQueueListenerLossy normalization of IPv4-mapped IPv6 destination endpoint.
    /// </summary>
    [TestMethod]
    public void SendingQueueListenerLossy_Ipv6MappedAddress_NormalizesAndSends()
    {
        using UdpClient udp = new(new IPEndPoint(IPAddress.Loopback, 0));
        SendingQueue queue = new();

        using SendingQueueListenerLossy listener = new(queue, udp);
        IPEndPoint ipv6MappedEp = new(IPAddress.Parse("::ffff:127.0.0.1"), GetAvailablePort());
        Packet packet = new(new byte[] { 1, 2, 3 }, ipv6MappedEp, 1, 1);
        queue.Enqueue(packet);
        Thread.Sleep(150);
    }

    /// <summary>
    /// Checks SendingQueueListenerLossy handling when underlying UdpClient is disposed.
    /// </summary>
    [TestMethod]
    public void SendingQueueListenerLossy_DisposedUdp_CatchesException()
    {
        UdpClient disposedUdp = new(new IPEndPoint(IPAddress.Loopback, 0));
        disposedUdp.Dispose();

        SendingQueue queue = new();
        using SendingQueueListenerLossy listener = new(queue, disposedUdp);
        Packet packet = new(new byte[] { 1 }, new IPEndPoint(IPAddress.Loopback, GetAvailablePort()), 1, 1);
        queue.Enqueue(packet);
        Thread.Sleep(150);
    }

    /// <summary>
    /// Checks CommunicatorServer lossy methods when server has not been started.
    /// </summary>
    [TestMethod]
    public void CommunicatorServer_LossyWhenNotStarted_ReturnsGracefully()
    {
        CommunicatorServer unstartedServer = new(new IPEndPoint(IPAddress.Loopback, 0));
        typeof(CommunicatorServer).GetField("_thisIpEndPoint", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(unstartedServer, null);

        unstartedServer.BroadcastMessageLossy(new byte[] { 1, 2, 3 }, 1, 1);
        unstartedServer.MulticastMessageLossy(1, new byte[] { 1, 2, 3 }, 1, 1);
        unstartedServer.Dispose();
    }

    /// <summary>
    /// Checks ReceivingQueueListenerLossy when enqueued packet is null.
    /// </summary>
    [TestMethod]
    public void ReceivingQueueListenerLossy_NullPacket_ContinuesListening()
    {
        ReceivingQueue queue = new();
        ConcurrentDictionary<int, IMessageListenerLossy> map = new();
        using ReceivingQueueListenerLossy listener = new(queue, map);

        queue.Enqueue(null!);
        Thread.Sleep(100);
    }

    /// <summary>
    /// Checks SendingQueueListenerLossy when enqueued packet is null.
    /// </summary>
    [TestMethod]
    public void SendingQueueListenerLossy_NullPacket_ContinuesListening()
    {
        using UdpClient udp = new(new IPEndPoint(IPAddress.Loopback, 0));
        SendingQueue queue = new();

        using SendingQueueListenerLossy listener = new(queue, udp);
        queue.Enqueue(null!);
        Thread.Sleep(100);
    }
}
