using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

public class SendingQueueListenerServer : IDisposable
{
    private SendingQueue _sendingQueue;

    private bool _isRunning;
    private object _isRunningLock;

    private bool IsRunning
    {
        get {
            lock (_isRunningLock)
            {
                return _isRunning;
            }
        }
    }
    private Thread _listeningThread;

    // Map for client to TCPClient
    private ConcurrentDictionary<IPEndPoint, TcpClient> _clientToTcpListenerMap;

    private PacketSerializer _packetSerializer;
    public SendingQueueListenerServer(SendingQueue sendingQueue, ConcurrentDictionary<IPEndPoint, TcpClient> clientToTcpListenerMap)
    {
        _isRunningLock = new object();
        _sendingQueue = sendingQueue;
        _clientToTcpListenerMap = clientToTcpListenerMap;
        _isRunning = true;
        _packetSerializer = new();
        _listeningThread = new Thread(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();
    }

    public void ListenOnQueue()
    {
        while (IsRunning)
        {
            if (_sendingQueue.IsEmpty())
            {
                Thread.Sleep(100);
                continue;
            }

            Packet? packet = _sendingQueue.Dequeue();
            if (packet == null)
            {
                continue;
            }

            byte[] data = _packetSerializer.PacketToBytes(packet);

            SendDataToClient(data, packet.Destination);

        }
    }

    public void SendDataToClient(byte[] data, IPEndPoint destination)
    {
        if (destination == null || !_clientToTcpListenerMap.TryGetValue(destination, out TcpClient? socket) || socket == null)
        {
            Trace.WriteLine($"[Networking] SendingQueueListenerServer.SendDataToClient: Destination client {destination} not found in client map.");
            return;
        }
        int maxAttempts = 4;
        int delay = 3000;  // ms
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                NetworkStream stream = socket.GetStream();
                stream.Write(data, 0, data.Length);
                return;
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[Networking] SendingQueueListenerServer.SendDataToClient: Unable to send data to client {destination} (Attempt {attempt}/{maxAttempts}). Retrying in {delay}ms. Exception: {e.Message}");
                Thread.Sleep(delay);
            }
        }
        _clientToTcpListenerMap.TryRemove(destination, out _);
        Trace.WriteLine($"[Networking] SendingQueueListenerServer.SendDataToClient: Max retry attempts reached. Client {destination} disconnected and removed.");
    }

    public void Dispose()
    {
        lock (_isRunningLock)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
        }
    }

}
