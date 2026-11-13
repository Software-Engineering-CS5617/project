using System;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Test;

/// <summary>
/// Unit tests for base Queue class.
/// </summary>
[TestClass]
public class QueueTests
{
    private static readonly IPEndPoint DummyEndpoint = new(IPAddress.Loopback, 8080);

    private static Packet CreatePacket(string content = "test")
    {
        return new Packet(Encoding.UTF8.GetBytes(content), DummyEndpoint, 1, 1);
    }

    /// <summary>
    /// Checks that newly created queue is empty.
    /// </summary>
    [TestMethod]
    public void Queue_Initialization_IsEmpty()
    {
        Queue queue = new();

        Assert.IsTrue(queue.IsEmpty());
        Assert.AreEqual(0, queue.Size());
        Assert.IsNull(queue.Peek());
        Assert.IsNull(queue.Dequeue());
    }

    /// <summary>
    /// Checks enqueuing, peeking, and dequeuing items in FIFO order.
    /// </summary>
    [TestMethod]
    public void Queue_EnqueueAndDequeue_MaintainsFifo()
    {
        Queue queue = new();
        Packet packet1 = CreatePacket("first");
        Packet packet2 = CreatePacket("second");

        queue.Enqueue(packet1);
        queue.Enqueue(packet2);

        Assert.IsFalse(queue.IsEmpty());
        Assert.AreEqual(2, queue.Size());

        Assert.AreEqual(packet1, queue.Peek());
        Assert.AreEqual(2, queue.Size());

        Assert.AreEqual(packet1, queue.Dequeue());
        Assert.AreEqual(1, queue.Size());

        Assert.AreEqual(packet2, queue.Dequeue());
        Assert.IsTrue(queue.IsEmpty());
        Assert.IsNull(queue.Dequeue());
    }

    /// <summary>
    /// Checks clearing the queue empties all contents.
    /// </summary>
    [TestMethod]
    public void Queue_Clear_EmptiesAllItems()
    {
        Queue queue = new();
        queue.Enqueue(CreatePacket("item1"));
        queue.Enqueue(CreatePacket("item2"));

        queue.Clear();

        Assert.IsTrue(queue.IsEmpty());
        Assert.AreEqual(0, queue.Size());
        Assert.IsNull(queue.Peek());
    }
}
