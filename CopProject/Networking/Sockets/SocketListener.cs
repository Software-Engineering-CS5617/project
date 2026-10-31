using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

public class SocketListener : IDisposable
{
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
    private ReceivingQueue _receivingQueue;
    private TcpClient _tcpClient;

    private const int BufferSize = 32 * 1024 * 1024;
    //private byte[] _buffer = new byte[_bufferSize];
    private byte[] _receivedBuffer = new byte[BufferSize];
    private PacketSerializer _packetSerializer = new PacketSerializer();


    public SocketListener(ReceivingQueue receivingQueue, TcpClient tcpClient)
    {
        _isRunningLock = new object();
        _receivingQueue = receivingQueue;
        _tcpClient = tcpClient;
        _receivedBuffer = new byte[BufferSize];
        _isRunning = true;
        NetworkStream stream = tcpClient.GetStream();
        _listeningThread = new Thread(() => ListenStream(stream));
        _listeningThread.Start();
    }

    public void ListenStream(NetworkStream stream)
    {
        using MemoryStream incomingData = new MemoryStream();
        try
        {
            while (_isRunning && _tcpClient.Connected)
            {
                int bytesRead = stream.Read(_receivedBuffer, 0, BufferSize);
                if (bytesRead == 0)
                {
                    Trace.WriteLine("[Networking] Host disconnected.");
                    break;
                }
                incomingData.Seek(0, SeekOrigin.End);
                incomingData.Write(_receivedBuffer, 0, bytesRead);

                ProcessData(incomingData);


            }
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] Error in {this.GetType().Name}: " + e);
        }

    }

    public void ProcessData(MemoryStream input)
    {
        byte[] buffer = input.GetBuffer();
        int length = (int)input.Length;
        int offset = 0;
        while (length - offset >= 4)
        {
            int payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(offset, 4));
            if (length - offset < 4 + payloadLength)
            {
                break;
            }

            byte[] packetData = new byte[4 + payloadLength];
            Buffer.BlockCopy(buffer, offset, packetData, 0, 4 + payloadLength);

            Packet? packet = _packetSerializer.BytesToPacket(packetData);
            if (packet != null)
            {
                _receivingQueue.Enqueue(packet);
            }
            offset += 4 + payloadLength;
        }

        if (offset > 0)
        {
            int remaining = length - offset;
            byte[] remainingBytes = new byte[remaining];
            Buffer.BlockCopy(buffer, offset, remainingBytes, 0, remaining);
            input.SetLength(0);
            input.Write(remainingBytes, 0, remaining);
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
