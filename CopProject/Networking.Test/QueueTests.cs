using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Test;

/// <summary>
/// Unit tests for Queue.
/// </summary>
[TestClass]
public class QueueTests
{
    private static Packet CreateDummyPacket(int module = 1, int priority = 1)
    {
        byte[] payload = new byte[] { 0x01, 0x02, 0x03 };
        IPEndPoint destination = new(IPAddress.Loopback, 8080);
        return new Packet(payload, destination, module, priority);
    }

    /// <summary>
    /// Checks FIFO ordering of packets in the queue.
    /// </summary>
    [TestMethod]
    public void Enqueue_MultiplePackets_DequeuesInFifoOrder()
    {
        Queue queue = new();
        Packet first = CreateDummyPacket(module: 1);
        Packet second = CreateDummyPacket(module: 2);

        queue.Enqueue(first);
        queue.Enqueue(second);

        Packet? dequeuedFirst = queue.Dequeue();
        Packet? dequeuedSecond = queue.Dequeue();

        Assert.IsNotNull(dequeuedFirst);
        Assert.IsNotNull(dequeuedSecond);
        Assert.AreEqual(1, dequeuedFirst.Module);
        Assert.AreEqual(2, dequeuedSecond.Module);
    }

    /// <summary>
    /// Checks that Size and IsEmpty update as packets are added.
    /// </summary>
    [TestMethod]
    public void IsEmptyAndSize_ReflectCurrentQueueState()
    {
        Queue queue = new();

        Assert.IsTrue(queue.IsEmpty());
        Assert.AreEqual(0, queue.Size());

        queue.Enqueue(CreateDummyPacket());

        Assert.IsFalse(queue.IsEmpty());
        Assert.AreEqual(1, queue.Size());
    }

    /// <summary>
    /// Checks that Peek returns the first packet without removing it.
    /// </summary>
    [TestMethod]
    public void Peek_ReturnsTopPacketWithoutRemoving()
    {
        Queue queue = new();
        Packet packet = CreateDummyPacket(module: 5);
        queue.Enqueue(packet);

        Packet? peeked = queue.Peek();

        Assert.IsNotNull(peeked);
        Assert.AreEqual(5, peeked.Module);
        Assert.AreEqual(1, queue.Size());
    }

    /// <summary>
    /// Checks that Clear empties the queue.
    /// </summary>
    [TestMethod]
    public void Clear_RemovesAllEnqueuedPackets()
    {
        Queue queue = new();
        queue.Enqueue(CreateDummyPacket());
        queue.Enqueue(CreateDummyPacket());

        queue.Clear();

        Assert.IsTrue(queue.IsEmpty());
        Assert.AreEqual(0, queue.Size());
        Assert.IsNull(queue.Dequeue());
    }

    /// <summary>
    /// Checks that Dequeue and Peek return null on an empty queue.
    /// </summary>
    [TestMethod]
    public void DequeueAndPeek_EmptyQueue_ReturnsNull()
    {
        Queue queue = new();

        Packet? dequeued = queue.Dequeue();
        Packet? peeked = queue.Peek();

        Assert.IsNull(dequeued);
        Assert.IsNull(peeked);
    }
}
