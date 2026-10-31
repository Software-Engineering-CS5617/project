using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Networking;

namespace FileSync.ConsoleApp;

/// <summary>
/// simple tcp communicator implementation of ICommunicator for testing network sync
/// </summary>
public class SimulatedCommunicator : ICommunicator, IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<int, IMessageListener> _subscribers = new();
    private readonly ConcurrentBag<IPEndPoint> _knownPeers = new();
    private bool _disposed;

    public int ListenPort { get; }

    /// <summary>
    /// initializes the TCP communicator, starts the listener, and launches the background listening loop
    /// </summary>
    public SimulatedCommunicator(int port)
    {
        ListenPort = port;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        // start background loop to listen for incoming peer messages
        _ = Task.Run(ListenLoopAsync);
    }

    /// <summary>
    /// registers a peer endpoint so that broadcast messages can be sent to it
    /// </summary>
    public void RegisterPeer(IPEndPoint peer)
    {
        _knownPeers.Add(peer);
    }

    /// <summary>
    /// registers a message listener for a specific module
    /// </summary>
    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        _subscribers[module] = subscriber;
    }

    /// <summary>
    /// removes the registered message listener for a specific module
    /// </summary>
    public void RemoveSubscriber(int module)
    {
        _subscribers.TryRemove(module, out _);
    }

    /// <summary>
    /// sends a message to a specific peer using a TCP connection
    /// </summary>
    public void SendMessage(IPEndPoint ipEndPoint, byte[] message, int module, int priority)
    {
        if (_disposed)
        {
            return;
        }
        try
        {
            // connect to the target peer and send the packet
            using var client = new TcpClient();
            client.Connect(ipEndPoint.Address, ipEndPoint.Port);
            using NetworkStream stream = client.GetStream();
            using var writer = new BinaryWriter(stream);

            // packet structure: module id, length, message bytes
            writer.Write(module);
            writer.Write(message.Length);
            writer.Write(message);
            writer.Flush();
        }
        catch (Exception ex)
        {
            // print error if peer is unreachable
            Console.WriteLine($"[network] send error to {ipEndPoint}: {ex.Message}");
        }
    }

    /// <summary>
    /// sends a message to all registered peer endpoints
    /// </summary>
    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        // broadcast to all registered peers
        foreach (IPEndPoint peer in _knownPeers)
        {
            SendMessage(peer, message, module, priority);
        }
    }

    /// <summary>
    /// joins the specified multicast group
    /// </summary>
    public void JoinMulticastGroup(int multicastGroup) { }

    /// <summary>
    /// leaves the specified multicast group
    /// </summary>
    public void LeaveMulticastGroup(int multicastGroup) { }

    /// <summary>
    /// sends a message to the specified multicast group by broadcasting it to registered peers
    /// </summary>
    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
        BroadcastMessage(message, module, priority);
    }

    /// <summary>
    /// continuously listens for incoming TCP connections and dispatches connected clients for handling
    /// </summary>
    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = Task.Run(() => HandleClient(client));
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { break; }
        }
    }

    /// <summary>
    /// processes an incoming TCP client, reads the message packet, and forwards it to the registered subscriber
    /// </summary>
    private void HandleClient(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (var reader = new BinaryReader(stream))
        {
            try
            {
                while (client.Connected && stream.DataAvailable || stream.CanRead)
                {
                    int module = reader.ReadInt32();
                    int length = reader.ReadInt32();
                    byte[] payload = reader.ReadBytes(length);

                    // forward received bytes to the registered subscriber
                    if (_subscribers.TryGetValue(module, out IMessageListener? listener))
                    {
                        listener.OnMessageReceived(payload);
                    }
                    break;
                }
            }
            catch (EndOfStreamException) { }
            catch (IOException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[network] handle client error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// releases the TCP listener and cancellation resources and stops the communicator
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }
}
