// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.ViewModels;

namespace Cloud.Networking.Server.Test.ViewModels;

/// <summary>
/// Tests the DashboardItems
/// </summary>
/// <remarks>
/// These two tests: stored as given, compared by value [can be used many places]
/// <see cref="Models.TestLiveConnection" />
/// </remarks>
[TestClass]
public class TestDashboardItems
{
    /// <summary>Tests that a connection item stores its values without modification.</summary>
    [TestMethod]
    public void ConnectionItemValuesAreStoredAsGiven()
    {
        ConnectionItem item = new("conn-1", "03:04:05");

        Assert.AreEqual("conn-1", item.ConnectionId);
        Assert.AreEqual("03:04:05", item.ConnectedAt);
    }

    /// <summary>Tests that connection items are compared by value.</summary>
    [TestMethod]
    public void ConnectionItemsAreComparedByValue()
    {
        ConnectionItem item = new("conn-1", "03:04:05");

        Assert.AreEqual(new ConnectionItem("conn-1", "03:04:05"), item);
        Assert.AreNotEqual(new ConnectionItem("conn-2", "03:04:05"), item);
        Assert.AreNotEqual(new ConnectionItem("conn-1", "03:04:06"), item);
    }

    /// <summary>Tests that a message item stores its values without modification.</summary>
    [TestMethod]
    public void MessageItemValuesAreStoredAsGiven()
    {
        MessageItem item = new("1a2b3c4d", "03:04:05", "client-1", "hello", true);

        Assert.AreEqual("1a2b3c4d", item.Id);
        Assert.AreEqual("03:04:05", item.Time);
        Assert.AreEqual("client-1", item.From);
        Assert.AreEqual("hello", item.Content);
        Assert.IsTrue(item.IsFromServer);
    }

    /// <summary>Tests that message items are compared by value (with server flag also)</summary>
    [TestMethod]
    public void MessageItemsAreComparedByValue()
    {
        MessageItem item = new("1a2b3c4d", "03:04:05", "client-1", "hello", false);

        Assert.AreEqual(new MessageItem("1a2b3c4d", "03:04:05", "client-1", "hello", false), item);
        Assert.AreNotEqual(new MessageItem("1a2b3c4d", "03:04:05", "client-1", "hello", true), item);
        Assert.AreNotEqual(new MessageItem("1a2b3c4d", "03:04:05", "client-1", "bye", false), item);
    }
}
