using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

public class SendingQueueListenerClient : IDisposable
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

    private PacketSerializer _packetSerializer;
    private Thread _listeningThread;

    TcpClient _clientSocket;

    public SendingQueueListenerClient(SendingQueue sendingQueue, TcpClient clientSocket)
    {
        _isRunningLock = new object();
        this._sendingQueue = sendingQueue;
        this._clientSocket = clientSocket;
        _isRunning = true;
        _packetSerializer = new();
        _listeningThread = new(ListenOnQueue) { IsBackground = true };
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

            try
            {
                NetworkStream stream = _clientSocket.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[Networking] SendingQueueListenerClient.ListenOnQueue: Error sending data to server. Exception: {e.Message}");
            }
        }
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
