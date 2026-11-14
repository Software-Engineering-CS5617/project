using System;
using System.Net;
using System.Reflection;
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
    private static readonly IPEndPoint s_dummyEndpoint = new(IPAddress.Loopback, 8080);

    private static Packet CreatePacket(string content = "test")
    {
        return new Packet(Encoding.UTF8.GetBytes(content), s_dummyEndpoint, 1, 1);
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

    /// <summary>
    /// Checks that WaitForItem unblocks and returns true when an item is enqueued.
    /// </summary>
    [TestMethod]
    public void Queue_WaitForItem_SignalsWhenItemEnqueued()
    {
        using Queue queue = new();
        ManualResetEventSlim waitingStarted = new(false);
        bool waitResult = false;

        Thread waitingThread = new(() => {
            waitingStarted.Set();
            waitResult = queue.WaitForItem(2000);
        });
        waitingThread.Start();

        Assert.IsTrue(waitingStarted.Wait(1000));
        Thread.Sleep(30);

        queue.Enqueue(CreatePacket("signal_test"));
        waitingThread.Join(1000);

        Assert.IsTrue(waitResult);
    }

    /// <summary>
    /// Checks that WaitForItem times out and returns false on an empty queue.
    /// </summary>
    [TestMethod]
    public void Queue_WaitForItem_TimesOutWhenEmpty()
    {
        using Queue queue = new();
        bool waitResult = queue.WaitForItem(50);
        Assert.IsFalse(waitResult);
    }

    /// <summary>
    /// Checks that Clear resets the AutoResetEvent signal.
    /// </summary>
    [TestMethod]
    public void Queue_Clear_ResetsSignal()
    {
        using Queue queue = new();
        queue.Enqueue(CreatePacket("item"));
        queue.Clear();

        bool waitResult = queue.WaitForItem(50);
        Assert.IsFalse(waitResult);
    }

    /// <summary>
    /// Checks that Dispose handles WaitForItem and Enqueue safely.
    /// </summary>
    [TestMethod]
    public void Queue_Dispose_SafeHandling()
    {
        Queue queue = new();
        queue.Dispose();

        Assert.IsFalse(queue.WaitForItem(50));
        // Enqueue after dispose should not throw
        queue.Enqueue(CreatePacket("after_dispose"));
    }

    /// <summary>
    /// Checks that ResetItemAdded handles ObjectDisposedException gracefully when queue is disposed.
    /// </summary>
    [TestMethod]
    public void Queue_ResetItemAdded_HandlesDisposedEvent()
    {
        Queue queue = new();
        queue.Dispose();

        MethodInfo? method = typeof(Queue).GetMethod("ResetItemAdded", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        method.Invoke(queue, null);
    }
}
