// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;

namespace Cloud.Networking.Server.Test.Models;

/// <summary>
/// Tests the live connection record
/// </summary>
/// <remarks>
/// These two tests: stored as given, compared by value [can be used many places]
/// <see cref="ViewModels.TestDashboardItems"/>
/// </remarks>
[TestClass]
public class TestLiveConnection
{
    /// <summary>Tests that connection ID and connection time are stored without modification.</summary>
    [TestMethod]
    public void LiveConnectionValuesAreStoredAsGiven()
    {
        DateTimeOffset connectedAt = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        LiveConnection connection = new("conn-1", connectedAt);

        Assert.AreEqual("conn-1", connection.ConnectionId);
        Assert.AreEqual(connectedAt, connection.ConnectedAt);
    }

    /// <summary>Tests that connections are compared by value</summary>
    [TestMethod]
    public void LiveConnectionsAreComparedByValue()
    {
        DateTimeOffset connectedAt = DateTimeOffset.UtcNow;
        LiveConnection connection = new("conn-1", connectedAt);

        Assert.AreEqual(connection, new LiveConnection("conn-1", connectedAt));
        Assert.AreNotEqual(connection, new LiveConnection("conn-2", connectedAt));
        Assert.AreNotEqual(connection, new LiveConnection("conn-1", connectedAt.AddSeconds(1)));
    }
}
