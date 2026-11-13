// Author : Eswar

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileSynchroniser.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking;

namespace Filesync.Test.Network;

[TestClass]
public class SimulatedCommunicatorTest
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Helper listener that uses a manual reset event to wait for incoming network messages
    /// </summary>
    private class TestMessageListener : IMessageListener
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

    /// <summary>
    /// Finds a free available port on loopback
    /// </summary>
    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    // ---------- reflection helpers ----------

    /// <summary>
    /// Creates a connected pair: the client side and the server side (accepted) TcpClient
    /// </summary>
    private static (TcpClient client, TcpClient server) CreateConnectedPair()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            TcpClient server = listener.AcceptTcpClient();
            return (client, server);
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    /// Calls the private HandleClient method directly
    /// </summary>
    private static void InvokeHandleClient(SimulatedCommunicator communicator, TcpClient serverSide)
    {
        MethodInfo method = typeof(SimulatedCommunicator).GetMethod("HandleClient", NonPublicInstance)!;
        method.Invoke(communicator, new object[] { serverSide });
    }

    /// <summary>
    /// Builds a SimulatedCommunicator without running its constructor so the private fields can be controlled
    /// </summary>
    private static SimulatedCommunicator CreateBare(TcpListener listener, CancellationTokenSource cts)
    {
        var communicator = (SimulatedCommunicator)RuntimeHelpers.GetUninitializedObject(typeof(SimulatedCommunicator));
        typeof(SimulatedCommunicator).GetField("_listener", NonPublicInstance)!.SetValue(communicator, listener);
        typeof(SimulatedCommunicator).GetField("_cts", NonPublicInstance)!.SetValue(communicator, cts);
        return communicator;
    }

    /// <summary>
    /// Calls the private ListenLoopAsync method directly
    /// </summary>
    private static Task RunListenLoop(SimulatedCommunicator communicator)
    {
        MethodInfo method = typeof(SimulatedCommunicator).GetMethod("ListenLoopAsync", NonPublicInstance)!;
        return (Task)method.Invoke(communicator, null)!;
    }

    private static void WritePacket(TcpClient client, int module, byte[] payload)
    {
        NetworkStream stream = client.GetStream();
        var writer = new BinaryWriter(stream);
        writer.Write(module);
        writer.Write(payload.Length);
        writer.Write(payload);
        writer.Flush();
    }

    /// <summary>
    /// 1. Constructor initializes listening port and IP address correctly
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesPortAndIpAddress()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);

        Assert.AreEqual(port, communicator.ListenPort);
        Assert.AreEqual(IPAddress.Any, communicator.ListenIpAddress);
    }

    /// <summary>
    /// 2. Adding and removing subscribers works without error
    /// </summary>
    [TestMethod]
    public void AddSubscriber_And_RemoveSubscriber_ManagesSubscribers()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);
        var listener = new TestMessageListener();

        communicator.AddSubscriber(5, listener);
        communicator.RemoveSubscriber(5);

        // removing non-existent subscriber should also not throw
        communicator.RemoveSubscriber(99);
    }

    /// <summary>
    /// 3. Multicast and group methods can be called safely
    /// </summary>
    [TestMethod]
    public void MulticastAndGroupMethods_CanBeCalledWithoutException()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        communicator.JoinMulticastGroup(1, endpoint);
        communicator.LeaveMulticastGroup(1, endpoint);

        byte[] data = Encoding.UTF8.GetBytes("test multicast");
        communicator.MulticastMessage(1, data, 5, 1);
    }

    /// <summary>
    /// 4. Sending message to an unreachable peer catches exception gracefully
    /// </summary>
    [TestMethod]
    public void SendMessage_ToUnreachablePeer_HandlesErrorGracefully()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);

        int unreachablePort = GetAvailablePort();
        var unreachableEndpoint = new IPEndPoint(IPAddress.Loopback, unreachablePort);

        byte[] payload = Encoding.UTF8.GetBytes("ping");

        // should catch socket error internally without crashing
        communicator.SendMessage(unreachableEndpoint, payload, 5, 1);
    }

    /// <summary>
    /// 5. RegisterPeer adds peer to broadcast list and sends message
    /// </summary>
    [TestMethod]
    public void RegisterPeer_And_BroadcastMessage_SendsToPeer()
    {
        int port = GetAvailablePort();
        using var communicator = new SimulatedCommunicator(port);

        int peerPort = GetAvailablePort();
        communicator.RegisterPeer(new IPEndPoint(IPAddress.Loopback, peerPort));

        byte[] payload = Encoding.UTF8.GetBytes("broadcast test");
        communicator.BroadcastMessage(payload, 5, 1);
    }

    /// <summary>
    /// 6. Real loopback TCP message sending and receiving delivers payload to subscriber
    /// </summary>
    [TestMethod]
    public void SendMessage_OverLoopback_DeliversMessageToSubscriber()
    {
        int receiverPort = GetAvailablePort();
        int senderPort = GetAvailablePort();

        using var receiver = new SimulatedCommunicator(receiverPort);
        using var sender = new SimulatedCommunicator(senderPort);

        var listener = new TestMessageListener();
        receiver.AddSubscriber(5, listener);

        byte[] sentBytes = Encoding.UTF8.GetBytes("hello from sender");
        var receiverEndpoint = new IPEndPoint(IPAddress.Loopback, receiverPort);

        sender.SendMessage(receiverEndpoint, sentBytes, 5, 1);

        bool received = listener.WaitForMessage(5000);
        Assert.IsTrue(received, "Expected receiver to receive message within timeout");
        Assert.IsNotNull(listener.ReceivedData);
        string receivedText = Encoding.UTF8.GetString(listener.ReceivedData);
        Assert.AreEqual("hello from sender", receivedText);
    }

    /// <summary>
    /// 7. Dispose cleanly stops listener and can be safely called multiple times
    /// </summary>
    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes_AndBlocksFurtherSends()
    {
        int port = GetAvailablePort();
        var communicator = new SimulatedCommunicator(port);

        communicator.Dispose();
        // calling a second time should hit the _disposed early return
        communicator.Dispose();

        // sending after dispose should return immediately without attempting socket connection
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        communicator.SendMessage(endpoint, new byte[] { 1, 2, 3 }, 5, 1);
    }

    /// <summary>
    /// 8. Broadcast with no registered peers sends nothing and does not throw
    /// </summary>
    [TestMethod]
    public void BroadcastMessage_WithNoPeers_DoesNothing()
    {
        using var communicator = new SimulatedCommunicator(GetAvailablePort());

        communicator.BroadcastMessage(new byte[] { 1 }, 5, 1);
    }

    /// <summary>
    /// 9. Broadcast delivers the payload to a registered real peer
    /// </summary>
    [TestMethod]
    public void BroadcastMessage_DeliversToRegisteredPeer()
    {
        int receiverPort = GetAvailablePort();
        using var receiver = new SimulatedCommunicator(receiverPort);
        using var sender = new SimulatedCommunicator(GetAvailablePort());

        var listener = new TestMessageListener();
        receiver.AddSubscriber(5, listener);
        sender.RegisterPeer(new IPEndPoint(IPAddress.Loopback, receiverPort));

        sender.BroadcastMessage(Encoding.UTF8.GetBytes("to everyone"), 5, 1);

        Assert.IsTrue(listener.WaitForMessage(5000));
        Assert.AreEqual("to everyone", Encoding.UTF8.GetString(listener.ReceivedData!));
    }


    /// <summary>
    /// 10. HandleClient reads a packet that has already arrived (DataAvailable is true)
    /// </summary>
    [TestMethod]
    public void HandleClient_DataAlreadyAvailable_ForwardsToSubscriber()
    {
        using var communicator = new SimulatedCommunicator(GetAvailablePort());
        var listener = new TestMessageListener();
        communicator.AddSubscriber(5, listener);

        (TcpClient client, TcpClient server) = CreateConnectedPair();
        using (client)
        {
            WritePacket(client, 5, Encoding.UTF8.GetBytes("early data"));
            Thread.Sleep(300); // let the bytes reach the server side socket

            InvokeHandleClient(communicator, server);
        }

        Assert.IsTrue(listener.WaitForMessage(2000));
        Assert.AreEqual("early data", Encoding.UTF8.GetString(listener.ReceivedData!));
    }

    /// <summary>
    /// 11. HandleClient waits for a packet that arrives later (DataAvailable is false at first)
    /// </summary>
    [TestMethod]
    public void HandleClient_DataArrivesLater_ForwardsToSubscriber()
    {
        using var communicator = new SimulatedCommunicator(GetAvailablePort());
        var listener = new TestMessageListener();
        communicator.AddSubscriber(5, listener);

        (TcpClient client, TcpClient server) = CreateConnectedPair();
        using (client)
        {
            Task sender = Task.Run(() => {
                Thread.Sleep(300);
                WritePacket(client, 5, Encoding.UTF8.GetBytes("late data"));
            });

            InvokeHandleClient(communicator, server);
            sender.Wait(2000);
        }

        Assert.IsTrue(listener.WaitForMessage(2000));
        Assert.AreEqual("late data", Encoding.UTF8.GetString(listener.ReceivedData!));
    }

    /// <summary>
    /// 12. HandleClient ignores packets for a module that has no subscriber
    /// </summary>
    [TestMethod]
    public void HandleClient_UnknownModule_IsIgnored()
    {
        using var communicator = new SimulatedCommunicator(GetAvailablePort());
        var listener = new TestMessageListener();
        communicator.AddSubscriber(5, listener);

        (TcpClient client, TcpClient server) = CreateConnectedPair();
        using (client)
        {
            WritePacket(client, 99, new byte[] { 1, 2, 3 });
            Thread.Sleep(200);

            InvokeHandleClient(communicator, server);
        }

        Assert.IsFalse(listener.WaitForMessage(300));
    }

    /// <summary>
    /// 13. HandleClient swallows EndOfStreamException when the peer closes without sending data
    /// </summary>
    [TestMethod]
    public void HandleClient_PeerClosesWithoutData_HandlesEndOfStream()
    {
        using var communicator = new SimulatedCommunicator(GetAvailablePort());

        (TcpClient client, TcpClient server) = CreateConnectedPair();
        client.Close();
        Thread.Sleep(200);

        InvokeHandleClient(communicator, server);
    }

    /// <summary>
    /// 14. HandleClient swallows IOException when the connection is reset by the peer
    /// </summary>
    [TestMethod]
    public void HandleClient_ConnectionReset_HandlesIOException()
    {
        using var communicator = new SimulatedCommunicator(GetAvailablePort());

        (TcpClient client, TcpClient server) = CreateConnectedPair();
        // zero linger makes Close send an RST instead of a normal FIN
        client.Client.LingerState = new LingerOption(true, 0);
        client.Close();
        Thread.Sleep(200);

        InvokeHandleClient(communicator, server);
    }

    /// <summary>
    /// 15. HandleClient logs and swallows unexpected exceptions such as a negative payload length
    /// </summary>
    [TestMethod]
    public void HandleClient_InvalidLength_HandlesGenericException()
    {
        using var communicator = new SimulatedCommunicator(GetAvailablePort());

        (TcpClient client, TcpClient server) = CreateConnectedPair();
        using (client)
        {
            var writer = new BinaryWriter(client.GetStream());
            writer.Write(5);   // module
            writer.Write(-1);  // invalid length -> ReadBytes throws ArgumentOutOfRangeException
            writer.Flush();
            Thread.Sleep(200);

            InvokeHandleClient(communicator, server);
        }
    }

    /// <summary>
    /// 16. HandleClient logs and swallows exceptions thrown by the subscriber
    /// </summary>
    [TestMethod]
    public void HandleClient_SubscriberThrows_HandlesGenericException()
    {
        using var communicator = new SimulatedCommunicator(GetAvailablePort());
        communicator.AddSubscriber(5, new ThrowingListener());

        (TcpClient client, TcpClient server) = CreateConnectedPair();
        using (client)
        {
            WritePacket(client, 5, new byte[] { 1 });
            Thread.Sleep(200);

            InvokeHandleClient(communicator, server);
        }
    }

    private class ThrowingListener : IMessageListener
    {
        public void OnMessageReceived(byte[] message)
        {
            throw new InvalidOperationException("listener failure");
        }
    }


    /// <summary>
    /// 17. Dispose while the listen loop is waiting for a client stops it cleanly
    /// </summary>
    [TestMethod]
    public void Dispose_WhileListening_StopsListenLoop()
    {
        var communicator = new SimulatedCommunicator(GetAvailablePort());
        Thread.Sleep(200); // let the loop reach AcceptTcpClientAsync

        communicator.Dispose();
    }

    /// <summary>
    /// 18. Listen loop exits through the OperationCanceledException handler
    /// </summary>
    [TestMethod]
    public async Task ListenLoop_CancelledWhileWaiting_ExitsGracefully()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var cts = new CancellationTokenSource();
        SimulatedCommunicator communicator = CreateBare(listener, cts);

        Task loop = RunListenLoop(communicator);
        await Task.Delay(300);
        cts.Cancel();

        Task finished = await Task.WhenAny(loop, Task.Delay(5000));
        Assert.AreSame(loop, finished, "Listen loop should stop after cancellation");
        await loop;
    }

    /// <summary>
    /// 19. Listen loop does not start when cancellation was already requested
    /// </summary>
    [TestMethod]
    public async Task ListenLoop_AlreadyCancelled_ReturnsImmediately()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        SimulatedCommunicator communicator = CreateBare(listener, cts);

        await RunListenLoop(communicator);
    }

    /// <summary>
    /// 20. Listen loop exits through the ObjectDisposedException handler when its token source is disposed
    /// </summary>
    [TestMethod]
    public async Task ListenLoop_TokenSourceDisposed_ExitsGracefully()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        var cts = new CancellationTokenSource();
        cts.Dispose(); // accessing cts.Token now throws ObjectDisposedException
        SimulatedCommunicator communicator = CreateBare(listener, cts);

        await RunListenLoop(communicator);
    }

}
