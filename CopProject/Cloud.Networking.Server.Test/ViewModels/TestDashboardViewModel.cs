// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Cloud.Networking.Server.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Cloud.Networking.Server.Test.ViewModels;

/// <summary>
/// Tests the DashBoard View Model
/// </summary>
[TestClass]
public class TestDashboardViewModel
{
    private Mock<IClientProxy> _all = null!;
    private Mock<IHubClients> _clients = null!;
    private Mock<IHubContext<MessageHub>> _hub = null!;
    private Mock<IServerMonitor> _monitor = null!;
    private DashboardViewModel _vm = null!;

    /// <summary>
    /// The view model only needs IHubContext.Clients.All, so only that is set up.
    /// </summary>
    [TestInitialize]
    public void Init()
    {
        _all = new Mock<IClientProxy>();
        _clients = new Mock<IHubClients>();
        _clients.SetupGet(c => c.All).Returns(_all.Object);
        _hub = new Mock<IHubContext<MessageHub>>();
        _hub.SetupGet(h => h.Clients).Returns(_clients.Object);
        _monitor = new Mock<IServerMonitor>();
        _monitor.SetupGet(m => m.Connections).Returns([]);
        _monitor.SetupGet(m => m.Messages).Returns([]);

        _vm = new DashboardViewModel(_monitor.Object, _hub.Object);
    }

    private void VerifyNothingWasBroadcast()
    {
        _all.Verify(a => a.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Tests that sending needs non-blank text and at least one connected client.</summary>
    [TestMethod]
    [DataRow("hello", true, true)]
    [DataRow("   ", true, false)]
    [DataRow("hello", false, false)]
    public void CanSendNeedsTextAndAConnectedClient(string text, bool hasConnection, bool expected)
    {
        if (hasConnection)
        {
            _monitor.SetupGet(m => m.Connections).Returns([new LiveConnection("conn-1", DateTimeOffset.UtcNow)]);
        }

        _vm.Text = text;
        Assert.AreEqual(expected, _vm.CanSend);
    }

    /// <summary>Tests that SendAsync does nothing when sending is not allowed.</summary>
    [TestMethod]
    public async Task SendAsyncDoesNothingWhenCanSendIsFalse()
    {
        // NOTE: No connected clients set up
        _vm.Text = "hello";

        await _vm.SendAsync();

        VerifyNothingWasBroadcast();
        _monitor.Verify(m => m.RecordMessage(It.IsAny<Message>()), Times.Never);
        Assert.IsNull(_vm.Status);
    }

    /// <summary>Tests that a trimmed server message is broadcast, recorded and the input is cleared.</summary>
    [TestMethod]
    public async Task SendAsyncBroadcastsRecordsAndClearsText()
    {
        object?[]? sentArgs = null;
        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => sentArgs = args)
            .Returns(Task.CompletedTask);

        _monitor.SetupGet(m => m.Connections).Returns([new LiveConnection("conn-1", DateTimeOffset.UtcNow)]);
        _vm.Text = "  hello  ";

        await _vm.SendAsync();

        Message sent = (Message)sentArgs!.Single()!;
        Assert.AreEqual("hello", sent.Content);
        Assert.AreEqual(ServerMonitor.ServerSenderId, sent.SenderId);
        _monitor.Verify(m => m.RecordMessage(sent), Times.Once);
        Assert.AreEqual("", _vm.Text);
        Assert.AreEqual("Sent to 1 connected client(s).", _vm.Status);
        Assert.IsFalse(_vm.StatusIsError);
    }

    /// <summary>Tests that CanSend is false while the send is still in flight.</summary>
    [TestMethod]
    public async Task CanSendIsFalseWhileSending()
    {
        bool canSendDuringSend = true;
        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, _, _) => canSendDuringSend = _vm.CanSend)
            .Returns(Task.CompletedTask);

        _monitor.SetupGet(m => m.Connections).Returns([new LiveConnection("conn-1", DateTimeOffset.UtcNow)]);
        _vm.Text = "hello";

        await _vm.SendAsync();

        Assert.IsFalse(canSendDuringSend);
    }

    /// <summary>Tests that a failed broadcast shows an error, keeps the text and allows retrying.</summary>
    [TestMethod]
    public async Task SendAsyncReportsFailureAndAllowsRetry()
    {
        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        _monitor.SetupGet(m => m.Connections).Returns([new LiveConnection("conn-1", DateTimeOffset.UtcNow)]);
        _vm.Text = "hello";

        await _vm.SendAsync();

        Assert.AreEqual("Failed to send: boom", _vm.Status);
        Assert.IsTrue(_vm.StatusIsError);
        Assert.AreEqual("hello", _vm.Text);
        _monitor.Verify(m => m.RecordMessage(It.IsAny<Message>()), Times.Never);
        Assert.IsTrue(_vm.CanSend);
    }

    /// <summary>Tests that monitor changes are forwarded and that Dispose stops the forwarding.</summary>
    [TestMethod]
    public void MonitorChangesAreForwardedUntilDisposed()
    {
        int raised = 0;
        _vm.Changed += () => raised++;

        _monitor.Raise(m => m.Changed += null);
        Assert.AreEqual(1, raised);

        // Dispose unsubscribes
        _vm.Dispose();
        _monitor.Raise(m => m.Changed += null);
        Assert.AreEqual(1, raised);
    }

    /// <summary>Tests that connections and messages are converted to the display format.</summary>
    [TestMethod]
    public void ConnectionsAndMessagesAreMappedForDisplay()
    {
        DateTimeOffset timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        Message fromClient = new() { SenderId = "client-1", Content = "hi", Timestamp = timestamp };
        Message fromServer = new() { SenderId = ServerMonitor.ServerSenderId, Content = "welcome" };

        _monitor.SetupGet(m => m.Connections).Returns([new LiveConnection("conn-1", timestamp)]);
        _monitor.SetupGet(m => m.Messages).Returns([fromServer, fromClient]);

        ConnectionItem connection = _vm.Connections.Single();
        Assert.AreEqual("conn-1", connection.ConnectionId);
        Assert.AreEqual(timestamp.ToLocalTime().ToString("HH:mm:ss"), connection.ConnectedAt);

        Assert.AreEqual(2, _vm.Messages.Count);
        Assert.IsTrue(_vm.Messages[0].IsFromServer);

        MessageItem item = _vm.Messages[1];
        Assert.AreEqual(fromClient.Id.ToString("N")[..8], item.Id);
        Assert.AreEqual(timestamp.ToLocalTime().ToString("HH:mm:ss"), item.Time);
        Assert.AreEqual("client-1", item.From);
        Assert.AreEqual("hi", item.Content);
        Assert.IsFalse(item.IsFromServer);
    }

    /// <summary>Tests that sending raises Changed before and after the send operation.</summary>
    [TestMethod]
    public async Task SendAsyncRaisesChangedBeforeAndAfterSending()
    {
        int raised = 0;
        _vm.Changed += () => raised++;

        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _monitor.SetupGet(m => m.Connections).Returns([new LiveConnection("conn-1", DateTimeOffset.UtcNow)]);
        _vm.Text = "hello";

        await _vm.SendAsync();

        Assert.AreEqual(2, raised);
    }
}
