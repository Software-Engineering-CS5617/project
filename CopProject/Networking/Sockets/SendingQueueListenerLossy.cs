using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

public class SendingQueueListenerLossy : IDisposable
{
    private readonly SendingQueue _sendingQueue;
    private readonly UdpClient _udpClient;
    private readonly PacketSerializer _packetSerializer = new();
    private readonly Thread _listeningThread;
    private readonly object _isRunningLock = new();
    private bool _isRunning;

    private bool IsRunning
    {
        get {
            lock (_isRunningLock)
            {
                return _isRunning;
            }
        }
    }

    public SendingQueueListenerLossy(SendingQueue sendingQueue, UdpClient udpClient)
    {
        _sendingQueue = sendingQueue;
        _udpClient = udpClient;
        try
        {
            _udpClient.EnableBroadcast = true;
            _udpClient.MulticastLoopback = true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[Networking] SendingQueueListenerLossy: Could not configure broadcast/multicast options: {ex.Message}");
        }

        _isRunning = true;
        _listeningThread = new Thread(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();
    }

    public void ListenOnQueue()
    {
        while (IsRunning)
        {
            if (_sendingQueue.IsEmpty())
            {
                _sendingQueue.WaitForItem(100);
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
                IPEndPoint dest = packet.Destination;
                if (dest.Address.IsIPv4MappedToIPv6)
                {
                    dest = new IPEndPoint(dest.Address.MapToIPv4(), dest.Port);
                }

                _udpClient.Send(data, data.Length, dest);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[Networking] SendingQueueListenerLossy: Error sending UDP packet to {packet.Destination}: {e.Message}");
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
