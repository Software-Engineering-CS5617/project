using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Sockets;

public class ReceivingQueueListenerLossy : IDisposable
{
    private readonly ConcurrentDictionary<int, IMessageListenerLossy> _moduleToMessageListenerMap;
    private readonly ReceivingQueue _receivingQueue;
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

    public ReceivingQueueListenerLossy(ReceivingQueue receivingQueue, ConcurrentDictionary<int, IMessageListenerLossy> moduleToMessageListenerMap)
    {
        _receivingQueue = receivingQueue;
        _moduleToMessageListenerMap = moduleToMessageListenerMap;
        _isRunning = true;
        _listeningThread = new Thread(ListenOnQueue) { IsBackground = true };
        _listeningThread.Start();
    }

    public void ListenOnQueue()
    {
        while (IsRunning)
        {
            if (_receivingQueue.IsEmpty())
            {
                _receivingQueue.WaitForItem(100);
                continue;
            }

            Packet? packet = _receivingQueue.Dequeue();
            if (packet == null)
            {
                continue;
            }

            if (_moduleToMessageListenerMap.TryGetValue(packet.Module, out IMessageListenerLossy? messageHandler) && messageHandler != null)
            {
                try
                {
                    messageHandler.OnMessageReceivedLossy(packet.SerializedData);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"[Networking] Lossy message handler for module {packet.Module} threw: {e}");
                }
            }
            else
            {
                Trace.WriteLine($"[Networking] Lossy Module {packet.Module} is not found. Unable to handle the data.");
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
