using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

public class UdpSocketListener : IDisposable
{
    private readonly ReceivingQueue _receivingQueue;
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

    public UdpSocketListener(ReceivingQueue receivingQueue, UdpClient udpClient)
    {
        _receivingQueue = receivingQueue;
        _udpClient = udpClient;
        _isRunning = true;
        _listeningThread = new Thread(ListenUdp) { IsBackground = true };
        _listeningThread.Start();
    }

    public void ListenUdp()
    {
        while (IsRunning)
        {
            try
            {
                IPEndPoint? remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpClient.Receive(ref remoteEp);
                if (data == null || data.Length == 0)
                {
                    continue;
                }

                Packet? packet = _packetSerializer.BytesToPacket(data);
                if (packet != null)
                {
                    _receivingQueue.Enqueue(packet);
                }
            }
            catch (SocketException e) when (!IsRunning || e.SocketErrorCode == SocketError.Interrupted || e.SocketErrorCode == SocketError.OperationAborted)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (!IsRunning)
                {
                    break;
                }
                Trace.WriteLine($"[Networking] UdpSocketListener exception: {e.Message}");
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

        try
        {
            _udpClient.Close();
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] UdpSocketListener.Dispose: Exception closing UdpClient: {e.Message}");
        }
    }
}
