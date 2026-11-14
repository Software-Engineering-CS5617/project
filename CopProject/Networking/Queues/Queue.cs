using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;

namespace Networking.Queues;

/// <summary>
/// Queue using locks
/// </summary>
public class Queue : IQueue, IDisposable
{
    private ConcurrentQueue<Packet> _queue;
    private readonly AutoResetEvent _itemAdded = new(false);
    private bool _disposed;

    public Queue()
    {
        this._queue = new ConcurrentQueue<Packet>();
    }


    public virtual void Enqueue(Packet packet)
    {
        Trace.WriteLine($"[Networking] Enqueuing to the queue {this.GetType().Name}");
        _queue.Enqueue(packet);
        SignalItemAdded();
    }

    public virtual Packet? Dequeue()
    {
        Trace.WriteLine($"[Networking] Dequeuing to the queue {this.GetType().Name}");
        if (!_queue.TryDequeue(out Packet? top))
        {
            Trace.WriteLine($"[Networking] Unable to dequeue {this.GetType().Name}, queue is empty");
        }
        return top;
    }


    public virtual Packet? Peek()
    {
        if (!_queue.TryPeek(out Packet? peek))
        {
            Trace.WriteLine($"[Networking] Unable to peek {this.GetType().Name}, queue is empty");
        }
        return peek;
    }

    public virtual void Clear()
    {
        _queue.Clear();
        ResetItemAdded();
    }

    public virtual bool WaitForItem(int timeoutMs)
    {
        try
        {
            return _itemAdded.WaitOne(timeoutMs);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    protected void SignalItemAdded()
    {
        try
        {
            _itemAdded.Set();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    protected void ResetItemAdded()
    {
        try
        {
            _itemAdded.Reset();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public virtual int Size()
    {
        return _queue.Count();
    }

    public virtual bool IsEmpty()
    {
        return _queue.IsEmpty;
    }

    public virtual void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _itemAdded.Dispose();
        }
    }
}
