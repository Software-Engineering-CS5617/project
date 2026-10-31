using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.TCPCommunication;

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
}
