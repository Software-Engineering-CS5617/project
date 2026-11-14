using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public void BroadcastMessageLossy_ClientToServer_ReflectsToOtherClients()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient clientA = new(serverEndpoint);
        TestLossyMessageListener listenerA = new();
        clientA.AddLossySubscriber(55, listenerA);

        using CommunicatorClient clientB = new(serverEndpoint);
        TestLossyMessageListener listenerB = new();
        clientB.AddLossySubscriber(55, listenerB);
        Thread.Sleep(250);

        byte[] payload = Encoding.UTF8.GetBytes("Client A Lossy Broadcast");
        clientA.BroadcastMessageLossy(payload, 55, 1);

        // Client B receives the reflected broadcast
        Assert.IsTrue(listenerB.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listenerB.ReceivedData);

        // Client A (the sender) is excluded from receiving its own broadcast
        Assert.IsFalse(listenerA.WaitForMessage(500));
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
    public void MulticastMessageLossy_ClientToGroupMembers_ReflectsThroughServer()
    {
        int port = GetAvailablePort();
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, port);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient clientA = new(serverEndpoint);

        using CommunicatorClient clientB = new(serverEndpoint);
        TestLossyMessageListener listenerB = new();
        clientB.AddLossySubscriber(65, listenerB);
        clientB.JoinMulticastGroup(77, new IPEndPoint(clientB.ListenIpAddress, clientB.ListenPort));
        Thread.Sleep(250);

        byte[] payload = Encoding.UTF8.GetBytes("Client A Multicast to Group 77");
        clientA.MulticastMessageLossy(77, payload, 65, 1);

        Assert.IsTrue(listenerB.WaitForMessage(3000));
        CollectionAssert.AreEqual(payload, listenerB.ReceivedData);
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
}
