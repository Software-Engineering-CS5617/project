// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Moq;

namespace Cloud.Networking.Server.Test.Models;

/// <summary>
/// Tests a server monitor 
/// </summary>
[TestClass]
public class TestServerMonitor
{
    private Mock<IMessageStore> _store = null!;
    private ServerMonitor _monitor = null!;

    /// <summary>The monitor only needs the store's MessageAdded event and GetRecentAsync.</summary>
    [TestInitialize]
    public void Init()
    {
        _store = new Mock<IMessageStore>();
        _monitor = new ServerMonitor(_store.Object);
    }

    private static Message NewMessage(string content)
    {
        return new() { SenderId = "sender-1", Content = content };
    }

    /// <summary>Tests that the monitor cannot be created without a message store.</summary>
    [TestMethod]
    public void NullStoreIsRejected()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ServerMonitor(null!));
    }

    /// <summary>Tests that blank connection IDs are rejected.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void BlankConnectionIdsAreRejected(string connectionId)
    {
        Assert.ThrowsExactly<ArgumentException>(() => _monitor.Connected(connectionId));
        Assert.ThrowsExactly<ArgumentException>(() => _monitor.Disconnected(connectionId));
    }

    /// <summary>Tests that connections are added on connect and removed on disconnect.</summary>
    [TestMethod]
    public void ConnectedAddsAndDisconnectedRemovesTheConnection()
    {
        _monitor.Connected("conn-1");
        _monitor.Connected("conn-2");
        _monitor.Disconnected("conn-1");

        Assert.AreEqual("conn-2", _monitor.Connections.Single().ConnectionId);
        Assert.AreEqual(1, _monitor.ConnectionCount);
    }

    /// <summary>Tests that connecting the same ID twice keeps a single entry.</summary>
    [TestMethod]
    public void ConnectingTheSameIdTwiceKeepsOneEntry()
    {
        _monitor.Connected("conn-1");
        _monitor.Connected("conn-1");

        Assert.AreEqual(1, _monitor.Connections.Count);
    }

    /// <summary>Tests that connections are returned ordered by connection time, oldest first.</summary>
    [TestMethod]
    public void ConnectionsAreOrderedByConnectionTime()
    {
        _monitor.Connected("conn-1");
        Thread.Sleep(20);
        _monitor.Connected("conn-2");
        Thread.Sleep(20);
        _monitor.Connected("conn-1");

        IReadOnlyList<LiveConnection> connections = _monitor.Connections;

        CollectionAssert.AreEqual(new[] { "conn-2", "conn-1" }, connections.Select(c => c.ConnectionId).ToArray());
        Assert.IsTrue(connections[0].ConnectedAt < connections[1].ConnectedAt);
    }

    /// <summary>Tests that recent messages come from the store.</summary>
    [TestMethod]
    public async Task GetRecentMessagesReturnsWhatTheStoreHas()
    {
        Message[] recent = [NewMessage("second"), NewMessage("first")];
        _store.Setup(s => s.GetRecentAsync(5)).ReturnsAsync(recent);

        IReadOnlyList<Message> result = await _monitor.GetRecentMessagesAsync(5);

        CollectionAssert.AreEqual(recent, result.ToArray());
    }

    /// <summary>Tests that Changed is raised once for each connect, disconnect and message added to the store.</summary>
    [TestMethod]
    public void ChangedIsRaisedOncePerOperation()
    {
        int raised = 0;
        _monitor.Changed += () => raised++;

        _monitor.Connected("conn-1");
        _store.Raise(s => s.MessageAdded += null, NewMessage("hello"));
        _monitor.Disconnected("conn-1");

        Assert.AreEqual(3, raised);
    }

    /// <summary>Tests that a message added to a real store makes the monitor raise Changed.</summary>
    [TestMethod]
    public async Task AddingAMessageToTheStoreRaisesChanged()
    {
        InMemoryMessageStore store = new();
        using ServerMonitor monitor = new(store);
        int raised = 0;
        monitor.Changed += () => raised++;

        await store.AddAsync(NewMessage("hello"));

        Assert.AreEqual(1, raised);
    }

    /// <summary>Tests that Dispose stops the monitor from listening to the store.</summary>
    [TestMethod]
    public void DisposeStopsListeningToTheStore()
    {
        int raised = 0;
        _monitor.Changed += () => raised++;

        _monitor.Dispose();
        _store.Raise(s => s.MessageAdded += null, NewMessage("hello"));

        Assert.AreEqual(0, raised);
    }
}
