using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;

namespace Networking.Queues;

public class SendingQueue : Queue
{
    public const int DefaultMediumWeightRatio = 3;
    public const int DefaultMaxHighBurst = 4;
    public const int DefaultMaxStarvationLimit = 10;

    private readonly ConcurrentQueue<Packet> _highQueue;
    private readonly ConcurrentQueue<Packet> _mediumQueue;
    private readonly ConcurrentQueue<Packet> _lowQueue;

    private readonly object _dequeueLock;

    private readonly int _mediumWeightRatio;
    private readonly int _maxHighBurst;
    private readonly int _maxStarvationLimit;

    private int _consecutiveHigherPriorityCount;
    private int _highBurstCount;
    private int _mediumServedCount;

    public SendingQueue(int mediumWeightRatio = DefaultMediumWeightRatio,
                        int maxHighBurst = DefaultMaxHighBurst,
                        int maxStarvationLimit = DefaultMaxStarvationLimit)
    {
        _highQueue = new ConcurrentQueue<Packet>();
        _mediumQueue = new ConcurrentQueue<Packet>();
        _lowQueue = new ConcurrentQueue<Packet>();

        _dequeueLock = new object();

        _mediumWeightRatio = mediumWeightRatio;
        _maxHighBurst = maxHighBurst;
        _maxStarvationLimit = maxStarvationLimit;

        _consecutiveHigherPriorityCount = 0;
        _highBurstCount = 0;
        _mediumServedCount = 0;
    }

    public static Priority NormalizePriority(int priority)
    {
        if (priority >= (int)Priority.High)
        {
            return Priority.High;
        }

        if (priority <= (int)Priority.Low)
        {
            return Priority.Low;
        }

        return Priority.Medium;
    }

    public override void Enqueue(Packet packet)
    {
        if (packet == null)
        {
            Trace.WriteLine($"[Networking] SendingQueue.Enqueue: Cannot enqueue null packet");
            return;
        }

        Priority priority = NormalizePriority(packet.Priority);
        Trace.WriteLine($"[Networking] Enqueuing to {GetType().Name} with priority {priority}");

        switch (priority)
        {
            case Priority.High:
                _highQueue.Enqueue(packet);
                break;
            case Priority.Medium:
                _mediumQueue.Enqueue(packet);
                break;
            case Priority.Low:
                _lowQueue.Enqueue(packet);
                break;
        }
    }

    public override Packet? Dequeue()
    {
        Trace.WriteLine($"[Networking] Dequeuing to the queue {GetType().Name}");

        lock (_dequeueLock)
        {
            if (_consecutiveHigherPriorityCount >= _maxStarvationLimit &&
                _lowQueue.TryDequeue(out Packet? starvedPacket))
            {
                _consecutiveHigherPriorityCount = 0;
                _mediumServedCount = 0;
                return starvedPacket;
            }

            bool allowHigh = _highBurstCount < _maxHighBurst || (_mediumQueue.IsEmpty && _lowQueue.IsEmpty);
            if (allowHigh && _highQueue.TryDequeue(out Packet? highPacket))
            {
                _highBurstCount++;
                if (!_lowQueue.IsEmpty)
                {
                    _consecutiveHigherPriorityCount++;
                }
                return highPacket;
            }

            bool allowMedium = _mediumServedCount < _mediumWeightRatio || _lowQueue.IsEmpty;
            if (allowMedium && _mediumQueue.TryDequeue(out Packet? mediumPacket))
            {
                _mediumServedCount++;
                _highBurstCount = 0;
                if (!_lowQueue.IsEmpty)
                {
                    _consecutiveHigherPriorityCount++;
                }
                return mediumPacket;
            }

            if (_lowQueue.TryDequeue(out Packet? lowPacket))
            {
                _mediumServedCount = 0;
                _highBurstCount = 0;
                _consecutiveHigherPriorityCount = 0;
                return lowPacket;
            }

            Trace.WriteLine($"[Networking] Unable to dequeue {GetType().Name}, queue is empty");
            return null;
        }
    }

    public override Packet? Peek()
    {
        lock (_dequeueLock)
        {
            if (_consecutiveHigherPriorityCount >= _maxStarvationLimit &&
                _lowQueue.TryPeek(out Packet? starvedPacket))
            {
                return starvedPacket;
            }

            bool allowHigh = _highBurstCount < _maxHighBurst || (_mediumQueue.IsEmpty && _lowQueue.IsEmpty);
            if (allowHigh && _highQueue.TryPeek(out Packet? highPacket))
            {
                return highPacket;
            }

            bool allowMedium = _mediumServedCount < _mediumWeightRatio || _lowQueue.IsEmpty;
            if (allowMedium && _mediumQueue.TryPeek(out Packet? mediumPacket))
            {
                return mediumPacket;
            }

            if (_lowQueue.TryPeek(out Packet? lowPacket))
            {
                return lowPacket;
            }

            Trace.WriteLine($"[Networking] Unable to peek {GetType().Name}, queue is empty");
            return null;
        }
    }

    public override void Clear()
    {
        lock (_dequeueLock)
        {
            _highQueue.Clear();
            _mediumQueue.Clear();
            _lowQueue.Clear();

            _consecutiveHigherPriorityCount = 0;
            _highBurstCount = 0;
            _mediumServedCount = 0;
        }
    }

    public override int Size()
    {
        return _highQueue.Count + _mediumQueue.Count + _lowQueue.Count;
    }

    public override bool IsEmpty()
    {
        return _highQueue.IsEmpty && _mediumQueue.IsEmpty && _lowQueue.IsEmpty;
    }

    public int HighCount => _highQueue.Count;
    public int MediumCount => _mediumQueue.Count;
    public int LowCount => _lowQueue.Count;
}
