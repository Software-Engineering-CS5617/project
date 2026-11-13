using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Packets;
using Networking.Queues;
using Networking.Sockets;

namespace Networking.Test;

/// <summary>
/// Unit tests for socket and queue listener classes in Networking.Sockets.
/// </summary>
[TestClass]
public class SocketListenerTests
{
    private static int GetAvailablePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class FaultyMessageListener : IMessageListener
    {
        public ManualResetEventSlim Signal { get; } = new(false);

        public void OnMessageReceived(byte[] message)
        {
            Signal.Set();
            throw new InvalidOperationException("Simulated handler fault");
        }
    }

    /// <summary>
    /// Checks ReceivingQueueListener handling when a subscriber throws an exception.
    /// </summary>
    [TestMethod]
    public void ReceivingQueueListener_HandlerThrows_CatchesAndContinues()
    {
        ReceivingQueue receivingQueue = new();
        ConcurrentDictionary<int, IMessageListener> map = new();
        FaultyMessageListener faultyListener = new();
        map[42] = faultyListener;

        using ReceivingQueueListener listener = new(receivingQueue, map);

        Packet packet = new(Encoding.UTF8.GetBytes("boom"), new IPEndPoint(IPAddress.Loopback, 80), 42, 1);
        receivingQueue.Enqueue(packet);

        Assert.IsTrue(faultyListener.Signal.Wait(2000));
        listener.Dispose();
        listener.Dispose();
    }

    /// <summary>
    /// Checks SendingQueueListenerServer when destination client is missing or null.
    /// </summary>
    [TestMethod]
    public void SendingQueueListenerServer_MissingOrNullDestination_HandledSafely()
    {
        SendingQueue queue = new();
        ConcurrentDictionary<IPEndPoint, TcpClient> map = new();

        using SendingQueueListenerServer listener = new(queue, map);

        IPEndPoint unknownDestination = new(IPAddress.Loopback, 9999);
        Packet packet = new(Encoding.UTF8.GetBytes("payload"), unknownDestination, 1, 1);
        queue.Enqueue(packet);

        Packet nullDestPacket = new(Encoding.UTF8.GetBytes("payload"), null!, 1, 1);
        queue.Enqueue(nullDestPacket);

        Thread.Sleep(150);
        listener.Dispose();
        listener.Dispose();
    }

    /// <summary>
    /// Checks SendingQueueListenerClient multiple dispose calls.
    /// </summary>
    [TestMethod]
    public void SendingQueueListenerClient_MultipleDispose_DoesNotThrow()
    {
        SendingQueue queue = new();
        using TcpClient client = new();
        using SendingQueueListenerClient listener = new(queue, client);

        listener.Dispose();
        listener.Dispose();
    }

    /// <summary>
    /// Checks SocketListener ProcessData with complete, incomplete, and short buffers.
    /// </summary>
    [TestMethod]
    public void SocketListener_ProcessData_HandlesPartialAndCompleteBuffers()
    {
        int port = GetAvailablePort();
        using TcpListener server = new(IPAddress.Loopback, port);
        server.Start();

        ReceivingQueue receivingQueue = new();
        using TcpClient client = new();
        client.Connect(IPAddress.Loopback, port);
        using TcpClient accepted = server.AcceptTcpClient();

        using SocketListener listener = new(receivingQueue, client);

        PacketSerializer serializer = new();
        Packet packet = new(Encoding.UTF8.GetBytes("message"), new IPEndPoint(IPAddress.Loopback, 80), 1, 1);
        byte[] fullPacket = serializer.PacketToBytes(packet);

        using MemoryStream partialStream = new();
        partialStream.Write(fullPacket, 0, fullPacket.Length / 2);
        listener.ProcessData(partialStream);
        Assert.IsTrue(receivingQueue.IsEmpty());

        partialStream.Write(fullPacket, fullPacket.Length / 2, fullPacket.Length - fullPacket.Length / 2);
        listener.ProcessData(partialStream);
        Assert.AreEqual(1, receivingQueue.Size());

        using MemoryStream tinyStream = new();
        tinyStream.Write(new byte[] { 1, 2 }, 0, 2);
        listener.ProcessData(tinyStream);
    }

    /// <summary>
    /// Checks ReceivingQueueListener dequeue handling when enqueued packet is null.
    /// </summary>
    [TestMethod]
    public void ReceivingQueueListener_NullPacket_ContinuesListening()
    {
        ReceivingQueue receivingQueue = new();
        ConcurrentDictionary<int, IMessageListener> map = new();
        using ReceivingQueueListener listener = new(receivingQueue, map);

        receivingQueue.Enqueue(null!);
        Thread.Sleep(150);
    }

    /// <summary>
    /// Checks SendingQueueListenerClient handling when packets are enqueued.
    /// </summary>
    [TestMethod]
    public void SendingQueueListenerClient_SendPacket_HandledSafely()
    {
        int port = GetAvailablePort();
        using TcpListener server = new(IPAddress.Loopback, port);
        server.Start();

        SendingQueue queue = new();
        using TcpClient client = new();
        client.Connect(IPAddress.Loopback, port);
        using TcpClient accepted = server.AcceptTcpClient();

        using SendingQueueListenerClient listener = new(queue, client);

        Packet packet = new(Encoding.UTF8.GetBytes("test_send"), new IPEndPoint(IPAddress.Loopback, port), 1, 1);
        queue.Enqueue(packet);
        Thread.Sleep(150);
    }

    /// <summary>
    /// Checks SendingQueueListenerServer handling with IPv4 mapped destination endpoint.
    /// </summary>
    [TestMethod]
    public void SendingQueueListenerServer_IPv6Mapped_HandledSafely()
    {
        SendingQueue queue = new();
        ConcurrentDictionary<IPEndPoint, TcpClient> map = new();
        using SendingQueueListenerServer listener = new(queue, map);

        IPEndPoint ipv6Mapped = new(IPAddress.Parse("::ffff:127.0.0.1"), 8888);
        Packet packet = new(Encoding.UTF8.GetBytes("test"), ipv6Mapped, 1, 1);
        queue.Enqueue(packet);

        Thread.Sleep(150);
    }
}
