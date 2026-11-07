// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;

namespace Cloud.Networking.Server.Test.Models;

/// <summary>
/// Tests the in-memory server monitor (it has no collaborators, so there is nothing to mock)
/// </summary>
[TestClass]
public class TestServerMonitor
{
    // Mirrors the private cap inside ServerMonitor.
    private const int MaxMessages = 200;

    private static Message NewMessage(string content)
    {
        return new() { SenderId = "sender-1", Content = content };
    }

    /// <summary>Tests that a null message is rejected.</summary>
    [TestMethod]
    public void NullMessageIsRejected()
    {
        ServerMonitor monitor = new();
        Assert.ThrowsExactly<ArgumentNullException>(() => monitor.RecordMessage(null!));
    }

    /// <summary>Tests that blank connection IDs are rejected.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void BlankConnectionIdsAreRejected(string connectionId)
    {
        ServerMonitor monitor = new();

        Assert.ThrowsExactly<ArgumentException>(() => monitor.Connected(connectionId));
        Assert.ThrowsExactly<ArgumentException>(() => monitor.Disconnected(connectionId));
    }

    /// <summary>Tests that connections are added on connect and removed on disconnect.</summary>
    [TestMethod]
    public void ConnectedAddsAndDisconnectedRemovesTheConnection()
    {
        ServerMonitor monitor = new();

        monitor.Connected("conn-1");
        monitor.Connected("conn-2");
        monitor.Disconnected("conn-1");

        Assert.AreEqual("conn-2", monitor.Connections.Single().ConnectionId);
    }

    /// <summary>Tests that connecting the same ID twice keeps a single entry.</summary>
    [TestMethod]
    public void ConnectingTheSameIdTwiceKeepsOneEntry()
    {
        ServerMonitor monitor = new();

        monitor.Connected("conn-1");
        monitor.Connected("conn-1");

        Assert.AreEqual(1, monitor.Connections.Count);
    }

    /// <summary>Tests that connections are returned ordered by connection time, oldest first.</summary>
    [TestMethod]
    public void ConnectionsAreOrderedByConnectionTime()
    {
        ServerMonitor monitor = new();

        monitor.Connected("conn-1");
        Thread.Sleep(20);
        monitor.Connected("conn-2");
        Thread.Sleep(20);
        monitor.Connected("conn-1");

        IReadOnlyList<LiveConnection> connections = monitor.Connections;

        CollectionAssert.AreEqual(new[] { "conn-2", "conn-1" }, connections.Select(c => c.ConnectionId).ToArray());
        Assert.IsTrue(connections[0].ConnectedAt < connections[1].ConnectedAt);
    }

    /// <summary>Tests that messages are returned newest first.</summary>
    [TestMethod]
    public void MessagesAreReturnedNewestFirst()
    {
        ServerMonitor monitor = new();
        Message first = NewMessage("first");
        Message second = NewMessage("second");

        monitor.RecordMessage(first);
        monitor.RecordMessage(second);

        CollectionAssert.AreEqual(new[] { second, first }, monitor.Messages.ToArray());
    }

    /// <summary>Tests that the oldest message is dropped once the cap is exceeded.</summary>
    [TestMethod]
    public void OldestMessageIsDroppedWhenCapIsExceeded()
    {
        ServerMonitor monitor = new();
        Message oldest = NewMessage("m0");
        monitor.RecordMessage(oldest);

        for (int i = 1; i <= MaxMessages; i++)
        {
            monitor.RecordMessage(NewMessage($"m{i}"));
        }

        Assert.AreEqual(MaxMessages, monitor.Messages.Count);
        Assert.IsFalse(monitor.Messages.Contains(oldest));
    }

    /// <summary>Tests that Changed is raised once for each connect, message and disconnect.</summary>
    [TestMethod]
    public void ChangedIsRaisedOncePerOperation()
    {
        ServerMonitor monitor = new();
        int raised = 0;
        monitor.Changed += () => raised++;

        monitor.Connected("conn-1");
        monitor.RecordMessage(NewMessage("hello"));
        monitor.Disconnected("conn-1");

        Assert.AreEqual(3, raised);
    }
}
