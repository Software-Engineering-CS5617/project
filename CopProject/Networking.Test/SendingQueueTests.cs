using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Packets;
using Networking.Queues;

namespace Networking.Test;

/// <summary>
/// Unit tests for SendingQueue.
/// </summary>
[TestClass]
public class SendingQueueTests
{
    private static readonly IPEndPoint DummyEndpoint = new(IPAddress.Loopback, 8080);

    private static Packet CreatePacket(string content, int priority, int module = 1)
    {
        byte[] payload = Encoding.UTF8.GetBytes(content);
        return new Packet(payload, DummyEndpoint, module, priority);
    }

    private static string GetPacketContent(Packet packet)
    {
        return Encoding.UTF8.GetString(packet.SerializedData);
    }

    /// <summary>
    /// Checks priority normalization into correct sub-queues.
    /// </summary>
    [TestMethod]
    public void Enqueue_PriorityNormalization_RoutesToCorrectSubQueue()
    {
        SendingQueue queue = new();

        queue.Enqueue(CreatePacket("Low_Neg", -5));
        queue.Enqueue(CreatePacket("Low_Zero", 0));
        queue.Enqueue(CreatePacket("Med_One", 1));
        queue.Enqueue(CreatePacket("Med_Two", 2));
        queue.Enqueue(CreatePacket("High_Three", 3));
        queue.Enqueue(CreatePacket("High_Five", 5));

        Assert.AreEqual(2, queue.LowCount);
        Assert.AreEqual(2, queue.MediumCount);
        Assert.AreEqual(2, queue.HighCount);
        Assert.AreEqual(6, queue.Size());
        Assert.IsFalse(queue.IsEmpty());
    }

    /// <summary>
    /// Checks high priority dequeues before medium and low.
    /// </summary>
    [TestMethod]
    public void Dequeue_StrictPriorityOrdering_ServesHighBeforeMediumAndLow()
    {
        SendingQueue queue = new();

        queue.Enqueue(CreatePacket("Low_Msg", (int)Priority.Low));
        queue.Enqueue(CreatePacket("Medium_Msg", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("High_Msg", (int)Priority.High));

        Packet? first = queue.Dequeue();
        Packet? second = queue.Dequeue();
        Packet? third = queue.Dequeue();

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.IsNotNull(third);

        Assert.AreEqual("High_Msg", GetPacketContent(first));
        Assert.AreEqual("Medium_Msg", GetPacketContent(second));
        Assert.AreEqual("Low_Msg", GetPacketContent(third));
        Assert.IsTrue(queue.IsEmpty());
    }

    /// <summary>
    /// Checks FIFO order within the same priority.
    /// </summary>
    [TestMethod]
    public void Dequeue_PreservesFifoWithinSamePriority()
    {
        SendingQueue queue = new();

        queue.Enqueue(CreatePacket("H1", (int)Priority.High));
        queue.Enqueue(CreatePacket("H2", (int)Priority.High));
        queue.Enqueue(CreatePacket("H3", (int)Priority.High));

        Assert.AreEqual("H1", GetPacketContent(queue.Dequeue()!));
        Assert.AreEqual("H2", GetPacketContent(queue.Dequeue()!));
        Assert.AreEqual("H3", GetPacketContent(queue.Dequeue()!));
    }

    /// <summary>
    /// Checks weighted round-robin interleaving between Medium and Low.
    /// </summary>
    [TestMethod]
    public void Dequeue_WeightedRoundRobin_InterleavesMediumAndLowCorrectly()
    {
        SendingQueue queue = new(mediumWeightRatio: 3, maxHighBurst: 4, maxStarvationLimit: 20);

        queue.Enqueue(CreatePacket("M1", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M2", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M3", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M4", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M5", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M6", (int)Priority.Medium));

        queue.Enqueue(CreatePacket("L1", (int)Priority.Low));
        queue.Enqueue(CreatePacket("L2", (int)Priority.Low));

        List<string> results = new();
        while (!queue.IsEmpty())
        {
            results.Add(GetPacketContent(queue.Dequeue()!));
        }

        CollectionAssert.AreEqual(new[] { "M1", "M2", "M3", "L1", "M4", "M5", "M6", "L2" }, results);
    }

    /// <summary>
    /// Checks high burst limit yields to medium.
    /// </summary>
    [TestMethod]
    public void Dequeue_HighBurstLimit_YieldsToMediumWhenBursted()
    {
        SendingQueue queue = new(mediumWeightRatio: 3, maxHighBurst: 2, maxStarvationLimit: 20);

        queue.Enqueue(CreatePacket("H1", (int)Priority.High));
        queue.Enqueue(CreatePacket("H2", (int)Priority.High));
        queue.Enqueue(CreatePacket("H3", (int)Priority.High));
        queue.Enqueue(CreatePacket("H4", (int)Priority.High));

        queue.Enqueue(CreatePacket("M1", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M2", (int)Priority.Medium));

        List<string> results = new();
        while (!queue.IsEmpty())
        {
            results.Add(GetPacketContent(queue.Dequeue()!));
        }

        CollectionAssert.AreEqual(new[] { "H1", "H2", "M1", "H3", "H4", "M2" }, results);
    }

    /// <summary>
    /// Checks low priority packet is promoted when starvation limit is reached.
    /// </summary>
    [TestMethod]
    public void Dequeue_StarvationPrevention_ForcesLowPriorityDelivery()
    {
        SendingQueue queue = new(mediumWeightRatio: 5, maxHighBurst: 5, maxStarvationLimit: 3);

        queue.Enqueue(CreatePacket("L1", (int)Priority.Low));

        queue.Enqueue(CreatePacket("M1", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M2", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M3", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M4", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M5", (int)Priority.Medium));

        Assert.AreEqual("M1", GetPacketContent(queue.Dequeue()!));
        Assert.AreEqual("M2", GetPacketContent(queue.Dequeue()!));
        Assert.AreEqual("M3", GetPacketContent(queue.Dequeue()!));
        Assert.AreEqual("L1", GetPacketContent(queue.Dequeue()!));
        Assert.AreEqual("M4", GetPacketContent(queue.Dequeue()!));
        Assert.AreEqual("M5", GetPacketContent(queue.Dequeue()!));
    }

    /// <summary>
    /// Checks peek behavior without removing packets.
    /// </summary>
    [TestMethod]
    public void Peek_ReturnsNextPacketWithoutMutatingQueue()
    {
        SendingQueue queue = new();

        Assert.IsNull(queue.Peek());

        queue.Enqueue(CreatePacket("Low", (int)Priority.Low));
        queue.Enqueue(CreatePacket("High", (int)Priority.High));

        Packet? peek1 = queue.Peek();
        Packet? peek2 = queue.Peek();

        Assert.IsNotNull(peek1);
        Assert.AreEqual("High", GetPacketContent(peek1));
        Assert.AreEqual("High", GetPacketContent(peek2!));
        Assert.AreEqual(2, queue.Size());

        Packet? dequeued = queue.Dequeue();
        Assert.AreEqual("High", GetPacketContent(dequeued!));
        Assert.AreEqual(1, queue.Size());

        Assert.AreEqual("Low", GetPacketContent(queue.Peek()!));
    }

    /// <summary>
    /// Checks peeking individual queues across priority tiers.
    /// </summary>
    [TestMethod]
    public void Peek_IndividualTiers_ReturnsExpectedPacket()
    {
        SendingQueue qHigh = new();
        qHigh.Enqueue(CreatePacket("H", (int)Priority.High));
        Assert.AreEqual("H", GetPacketContent(qHigh.Peek()!));

        SendingQueue qMed = new();
        qMed.Enqueue(CreatePacket("M", (int)Priority.Medium));
        Assert.AreEqual("M", GetPacketContent(qMed.Peek()!));

        SendingQueue qLow = new();
        qLow.Enqueue(CreatePacket("L", (int)Priority.Low));
        Assert.AreEqual("L", GetPacketContent(qLow.Peek()!));

        SendingQueue starvationQueue = new(mediumWeightRatio: 5, maxHighBurst: 5, maxStarvationLimit: 2);
        starvationQueue.Enqueue(CreatePacket("L1", (int)Priority.Low));
        starvationQueue.Enqueue(CreatePacket("M1", (int)Priority.Medium));
        starvationQueue.Enqueue(CreatePacket("M2", (int)Priority.Medium));
        starvationQueue.Dequeue();
        starvationQueue.Dequeue();

        Assert.AreEqual("L1", GetPacketContent(starvationQueue.Peek()!));
    }

    /// <summary>
    /// Checks peeking when medium cycle is exhausted and low has items.
    /// </summary>
    [TestMethod]
    public void Peek_MediumCycleExhausted_PeeksLowPacket()
    {
        SendingQueue queue = new(mediumWeightRatio: 1, maxHighBurst: 1, maxStarvationLimit: 10);
        queue.Enqueue(CreatePacket("M1", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("L1", (int)Priority.Low));
        queue.Enqueue(CreatePacket("M2", (int)Priority.Medium));

        queue.Dequeue();
        Assert.AreEqual("L1", GetPacketContent(queue.Peek()!));
    }

    /// <summary>
    /// Checks medium dequeues and peeks beyond ratio when low queue is empty.
    /// </summary>
    [TestMethod]
    public void DequeueAndPeek_MediumBeyondRatioWhenLowEmpty_ServesMedium()
    {
        SendingQueue queue = new(mediumWeightRatio: 2, maxHighBurst: 2, maxStarvationLimit: 10);
        queue.Enqueue(CreatePacket("M1", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M2", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M3", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("M4", (int)Priority.Medium));

        queue.Dequeue();
        queue.Dequeue();
        Assert.AreEqual("M3", GetPacketContent(queue.Peek()!));
        Assert.AreEqual("M3", GetPacketContent(queue.Dequeue()!));
        Assert.AreEqual("M4", GetPacketContent(queue.Dequeue()!));
    }

    /// <summary>
    /// Checks high dequeues and peeks beyond burst when medium and low are empty.
    /// </summary>
    [TestMethod]
    public void DequeueAndPeek_HighBeyondBurstWhenOthersEmpty_ServesHigh()
    {
        SendingQueue queue = new(mediumWeightRatio: 2, maxHighBurst: 2, maxStarvationLimit: 10);
        queue.Enqueue(CreatePacket("H1", (int)Priority.High));
        queue.Enqueue(CreatePacket("H2", (int)Priority.High));
        queue.Enqueue(CreatePacket("H3", (int)Priority.High));

        queue.Dequeue();
        queue.Dequeue();
        Assert.AreEqual("H3", GetPacketContent(queue.Peek()!));
        Assert.AreEqual("H3", GetPacketContent(queue.Dequeue()!));
    }

    /// <summary>
    /// Checks clearing the queue resets all state.
    /// </summary>
    [TestMethod]
    public void Clear_EmptiesAllSubQueuesAndResetsState()
    {
        SendingQueue queue = new();

        queue.Enqueue(CreatePacket("H", (int)Priority.High));
        queue.Enqueue(CreatePacket("M", (int)Priority.Medium));
        queue.Enqueue(CreatePacket("L", (int)Priority.Low));

        Assert.AreEqual(3, queue.Size());
        queue.Clear();

        Assert.AreEqual(0, queue.Size());
        Assert.IsTrue(queue.IsEmpty());
        Assert.IsNull(queue.Dequeue());
        Assert.IsNull(queue.Peek());
    }

    /// <summary>
    /// Checks concurrent enqueues from multiple threads.
    /// </summary>
    [TestMethod]
    public void ConcurrentEnqueue_AllPacketsAreSafelyEnqueuedAndDequeued()
    {
        SendingQueue queue = new();
        int producerCount = 6;
        int itemsPerProducer = 100;
        CountdownEvent latch = new(producerCount);

        Parallel.For(0, producerCount, p => {
            int priority = p % 4;
            for (int i = 0; i < itemsPerProducer; i++)
            {
                queue.Enqueue(CreatePacket($"Producer_{p}_Item_{i}", priority));
            }
            latch.Signal();
        });

        latch.Wait();

        Assert.AreEqual(producerCount * itemsPerProducer, queue.Size());

        int dequeuedCount = 0;
        while (!queue.IsEmpty())
        {
            Packet? pkt = queue.Dequeue();
            if (pkt != null)
            {
                dequeuedCount++;
            }
        }

        Assert.AreEqual(producerCount * itemsPerProducer, dequeuedCount);
        Assert.IsTrue(queue.IsEmpty());
    }

    /// <summary>
    /// Checks that WaitForItem signals when packets are enqueued at various priorities.
    /// </summary>
    [TestMethod]
    public void SendingQueue_WaitForItem_SignalsWhenPacketEnqueued()
    {
        using SendingQueue queue = new();

        // Enqueue High priority
        queue.Enqueue(CreatePacket("HighItem", 3));
        Assert.IsTrue(queue.WaitForItem(100));

        // Dequeue it
        Assert.IsNotNull(queue.Dequeue());

        // Queue is now empty, WaitForItem should time out
        Assert.IsFalse(queue.WaitForItem(50));

        // Enqueue Medium priority
        queue.Enqueue(CreatePacket("MedItem", 1));
        Assert.IsTrue(queue.WaitForItem(100));
        Assert.IsNotNull(queue.Dequeue());
        Assert.IsFalse(queue.WaitForItem(50));

        // Enqueue Low priority
        queue.Enqueue(CreatePacket("LowItem", 0));
        Assert.IsTrue(queue.WaitForItem(100));
        Assert.IsNotNull(queue.Dequeue());
        Assert.IsFalse(queue.WaitForItem(50));
    }

    /// <summary>
    /// Checks that SendingQueue.Clear resets the AutoResetEvent signal.
    /// </summary>
    [TestMethod]
    public void SendingQueue_Clear_ResetsSignal()
    {
        using SendingQueue queue = new();
        queue.Enqueue(CreatePacket("item", 2));
        queue.Clear();

        Assert.IsFalse(queue.WaitForItem(50));
    }
}
