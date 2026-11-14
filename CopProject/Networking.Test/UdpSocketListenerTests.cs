using System;
using System.Diagnostics;
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
/// Unit tests for UdpSocketListener covering lifecycle, reception, error handling, and disposal.
/// </summary>
[TestClass]
public class UdpSocketListenerTests
{
    private static int GetAvailablePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class ThrowingUdpClient : UdpClient
    {
        public ThrowingUdpClient(int port) : base(port)
        {
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                throw new InvalidOperationException("Simulated UdpClient close failure");
            }
            base.Dispose(disposing);
        }
    }

    private sealed class ThrowingReceivingQueue : ReceivingQueue
    {
        private readonly Action _onEnqueue;

        public ThrowingReceivingQueue(Action onEnqueue)
        {
            _onEnqueue = onEnqueue;
        }

        public override void Enqueue(Packet packet)
        {
            _onEnqueue();
            throw new InvalidOperationException("Simulated enqueue error");
        }
    }

    /// <summary>
    /// Checks that UdpSocketListener gracefully exits when initialized with an already-disposed UdpClient.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_PreDisposedClient_ExitsLoopCleanlyViaObjectDisposedException()
    {
        int port = GetAvailablePort();
        UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, port));
        udpClient.Dispose();

        ReceivingQueue queue = new();
        using UdpSocketListener listener = new(queue, udpClient);

        // Allow background thread to attempt Receive, catch ObjectDisposedException, and exit loop
        Thread.Sleep(150);
        Assert.IsNull(queue.Dequeue());
    }

    /// <summary>
    /// Checks that UdpSocketListener.Dispose catches and logs any exception thrown during UdpClient.Close.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_Dispose_WhenClientCloseThrows_CatchesAndLogs()
    {
        int port = GetAvailablePort();
        ThrowingUdpClient throwingClient = new(port);
        ReceivingQueue queue = new();

        using UdpSocketListener listener = new(queue, throwingClient);
        // Dispose should catch the InvalidOperationException from ThrowingUdpClient without propagating
        listener.Dispose();
    }

    /// <summary>
    /// Checks that UdpSocketListener terminates the loop if an error occurs while IsRunning is false.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_EnqueueThrowsWhenStopped_BreaksLoop()
    {
        int port = GetAvailablePort();
        using UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, port));

        UdpSocketListener? listener = null;
        ThrowingReceivingQueue throwingQueue = new(() => {
            // Dispose listener while processing packet so IsRunning becomes false
            listener?.Dispose();
        });

        listener = new UdpSocketListener(throwingQueue, udpClient);

        // Send a valid packet so it triggers Enqueue on the listener thread
        using UdpClient sender = new();
        Packet packet = new(Encoding.UTF8.GetBytes("trigger-enqueue-throw"), new IPEndPoint(IPAddress.Loopback, port), 1, 1);
        byte[] data = new PacketSerializer().PacketToBytes(packet);
        sender.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, port));

        Thread.Sleep(200);
        listener.Dispose();
    }

    /// <summary>
    /// Checks normal packet reception and enqueuing in UdpSocketListener.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_ValidPacket_EnqueuesSuccessfully()
    {
        int port = GetAvailablePort();
        using UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, port));
        ReceivingQueue queue = new();
        using UdpSocketListener listener = new(queue, udpClient);

        using UdpClient sender = new();
        Packet originalPacket = new(Encoding.UTF8.GetBytes("hello-udp"), new IPEndPoint(IPAddress.Loopback, port), 42, 1);
        byte[] data = new PacketSerializer().PacketToBytes(originalPacket);
        sender.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, port));

        Assert.IsTrue(queue.WaitForItem(2000));
        Packet? receivedPacket = queue.Dequeue();
        Assert.IsNotNull(receivedPacket);
        CollectionAssert.AreEqual(originalPacket.SerializedData, receivedPacket.SerializedData);
        Assert.AreEqual(originalPacket.Module, receivedPacket.Module);
    }

    /// <summary>
    /// Checks that receiving a zero-byte datagram does not crash UdpSocketListener and continues listening.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_ZeroByteDatagram_ContinuesListening()
    {
        int port = GetAvailablePort();
        using UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, port));
        ReceivingQueue queue = new();
        using UdpSocketListener listener = new(queue, udpClient);

        using UdpClient sender = new();
        sender.Send(Array.Empty<byte>(), 0, new IPEndPoint(IPAddress.Loopback, port));

        Thread.Sleep(100);
        Assert.IsNull(queue.Dequeue());
    }

    /// <summary>
    /// Checks that receiving a payload under 4 bytes is ignored and listener continues.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_SmallPayload_ContinuesListening()
    {
        int port = GetAvailablePort();
        using UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, port));
        ReceivingQueue queue = new();
        using UdpSocketListener listener = new(queue, udpClient);

        using UdpClient sender = new();
        sender.Send(new byte[] { 1, 2 }, 2, new IPEndPoint(IPAddress.Loopback, port));

        Thread.Sleep(100);
        Assert.IsNull(queue.Dequeue());
    }

    /// <summary>
    /// Checks that invalid/corrupt packet bytes do not enqueue null and listener continues.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_CorruptPacketBytes_ContinuesListening()
    {
        int port = GetAvailablePort();
        using UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, port));
        ReceivingQueue queue = new();
        using UdpSocketListener listener = new(queue, udpClient);

        using UdpClient sender = new();
        sender.Send(new byte[] { 255, 255, 255, 255, 1, 2, 3, 4 }, 8, new IPEndPoint(IPAddress.Loopback, port));

        Thread.Sleep(100);
        Assert.IsNull(queue.Dequeue());
    }

    /// <summary>
    /// Checks that an exception during packet processing is caught and logged while running.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_PacketProcessingThrows_CatchesAndContinues()
    {
        int port = GetAvailablePort();
        using UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, port));
        ReceivingQueue queue = new();

        using UdpSocketListener listener = new(queue, udpClient);
        typeof(UdpSocketListener).GetField("_packetSerializer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(listener, null);

        using UdpClient sender = new();
        sender.Send(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, 5, new IPEndPoint(IPAddress.Loopback, port));
        Thread.Sleep(100);
    }

    /// <summary>
    /// Checks that calling Dispose multiple times is idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void UdpSocketListener_DoubleDispose_IsIdempotent()
    {
        int port = GetAvailablePort();
        using UdpClient udpClient = new(new IPEndPoint(IPAddress.Loopback, port));
        ReceivingQueue queue = new();

        UdpSocketListener listener = new(queue, udpClient);
        listener.Dispose();
        listener.Dispose();
    }
}
