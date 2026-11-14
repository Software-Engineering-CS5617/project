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
    private Mock<IMessageStore> _store = null!;
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
        _store = new Mock<IMessageStore>();
        _monitor = new Mock<IServerMonitor>();
        _monitor.SetupGet(m => m.Connections).Returns([]);
        _monitor.Setup(m => m.GetRecentMessagesAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<Message>>([]));

        _vm = new DashboardViewModel(_monitor.Object, _store.Object, _hub.Object);
    }

    private void VerifyNothingWasStoredOrBroadcast()
    {
        _store.Verify(s => s.AddAsync(It.IsAny<Message>()), Times.Never);
        _all.Verify(a => a.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Tests that every dependency is required.</summary>
    [TestMethod]
    public void NullDependenciesAreRejected()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new DashboardViewModel(null!, _store.Object, _hub.Object));
        Assert.ThrowsExactly<ArgumentNullException>(() => new DashboardViewModel(_monitor.Object, null!, _hub.Object));
        Assert.ThrowsExactly<ArgumentNullException>(() => new DashboardViewModel(_monitor.Object, _store.Object, null!));
    }

    /// <summary>Tests that sending needs non-blank text.</summary>
    [TestMethod]
    [DataRow("hello", true)]
    [DataRow("   ", false)]
    [DataRow("", false)]
    public void CanSendNeedsNonBlankText(string text, bool expected)
    {
        _vm.Text = text;
        Assert.AreEqual(expected, _vm.CanSend);
    }

    /// <summary>Tests that the length limit applies to the trimmed text.</summary>
    [TestMethod]
    public void CanSendIsFalseOnlyWhenTrimmedTextExceedsMaxLength()
    {
        _vm.Text = $"  {new string('a', DashboardViewModel.MaxLength)}  ";
        Assert.IsTrue(_vm.CanSend);

        _vm.Text = new string('a', DashboardViewModel.MaxLength + 1);
        Assert.IsFalse(_vm.CanSend);
    }

    /// <summary>Tests that SendAsync does nothing when sending is not allowed.</summary>
    [TestMethod]
    public async Task SendAsyncDoesNothingWhenCanSendIsFalse()
    {
        _vm.Text = "   ";

        await _vm.SendAsync();

        VerifyNothingWasStoredOrBroadcast();
        Assert.IsNull(_vm.Status);
    }

    /// <summary>Tests that a trimmed server message is stored first, then broadcast, and the input is cleared.</summary>
    [TestMethod]
    public async Task SendAsyncStoresThenBroadcastsAndClearsText()
    {
        List<string> order = [];
        Message? stored = null;
        object?[]? sentArgs = null;

        _store.Setup(s => s.AddAsync(It.IsAny<Message>()))
            .Callback<Message>(m => {
                order.Add("store");
                stored = m;
            })
            .Returns(Task.CompletedTask);
        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => {
                order.Add("broadcast");
                sentArgs = args;
            })
            .Returns(Task.CompletedTask);

        _monitor.SetupGet(m => m.ConnectionCount).Returns(1);
        _vm.Text = "  hello  ";

        await _vm.SendAsync();

        CollectionAssert.AreEqual(new[] { "store", "broadcast" }, order);
        Message sent = (Message)sentArgs!.Single()!;
        Assert.AreSame(stored, sent);
        Assert.AreEqual("hello", sent.Content);
        Assert.AreEqual(MessageHub.ServerSenderId, sent.SenderId);
        Assert.AreEqual("", _vm.Text);
        Assert.AreEqual("Sent to 1 connected client(s). Offline clients get it when they sync.", _vm.Status);
        Assert.IsFalse(_vm.StatusIsError);
    }

    /// <summary>Tests that sending with nobody online still succeeds and says the message was only saved.</summary>
    [TestMethod]
    public async Task SendAsyncWithNoClientsOnlineReportsSavedOnly()
    {
        _monitor.SetupGet(m => m.ConnectionCount).Returns(0);
        _vm.Text = "hello";

        await _vm.SendAsync();

        _store.Verify(s => s.AddAsync(It.IsAny<Message>()), Times.Once);
        Assert.AreEqual("Saved. No clients are online; they will get it when they sync.", _vm.Status);
        Assert.IsFalse(_vm.StatusIsError);
        Assert.AreEqual("", _vm.Text);
    }

    /// <summary>Tests that CanSend is false while the send is still in flight.</summary>
    [TestMethod]
    public async Task CanSendIsFalseWhileSending()
    {
        bool canSendDuringSend = true;
        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, _, _) => canSendDuringSend = _vm.CanSend)
            .Returns(Task.CompletedTask);

        _vm.Text = "hello";

        await _vm.SendAsync();

        Assert.IsFalse(canSendDuringSend);
    }

    /// <summary>Tests that a failed broadcast shows an error, keeps the text (already stored) and allows retrying.</summary>
    [TestMethod]
    public async Task SendAsyncReportsFailureAndAllowsRetry()
    {
        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        _vm.Text = "hello";

        await _vm.SendAsync();

        Assert.AreEqual("Failed to send: boom", _vm.Status);
        Assert.IsTrue(_vm.StatusIsError);
        Assert.AreEqual("hello", _vm.Text);
        _store.Verify(s => s.AddAsync(It.IsAny<Message>()), Times.Once);
        Assert.IsTrue(_vm.CanSend);
    }

    /// <summary>Tests that sending raises Changed before and after the send operation.</summary>
    [TestMethod]
    public async Task SendAsyncRaisesChangedBeforeAndAfterSending()
    {
        int raised = 0;
        _vm.Changed += () => raised++;

        _vm.Text = "hello";

        await _vm.SendAsync();

        Assert.AreEqual(2, raised);
    }

    /// <summary>Tests that connections and recent messages are converted to the display format.</summary>
    [TestMethod]
    public async Task RefreshAsyncMapsConnectionsAndMessagesForDisplay()
    {
        DateTimeOffset timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        Message fromClient = new() { SenderId = "client-1", Content = "hi", Timestamp = timestamp };
        Message fromServer = new() { SenderId = MessageHub.ServerSenderId, Content = "welcome" };

        _monitor.SetupGet(m => m.Connections).Returns([new LiveConnection("conn-1", timestamp)]);
        _monitor.Setup(m => m.GetRecentMessagesAsync(DashboardViewModel.RecentMessageCount))
            .Returns(Task.FromResult<IReadOnlyList<Message>>([fromServer, fromClient]));

        int raised = 0;
        _vm.Changed += () => raised++;

        await _vm.RefreshAsync();

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

        Assert.AreEqual(1, raised);
    }

    /// <summary>Tests that when messages cannot be loaded the error is shown, the old messages are kept and connections still update.</summary>
    [TestMethod]
    public async Task RefreshAsyncReportsLoadFailureButStillUpdatesConnections()
    {
        Message existing = new() { SenderId = "client-1", Content = "hi" };
        _monitor.Setup(m => m.GetRecentMessagesAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<Message>>([existing]));
        await _vm.RefreshAsync();

        _monitor.SetupGet(m => m.Connections).Returns([new LiveConnection("conn-1", DateTimeOffset.UtcNow)]);
        _monitor.Setup(m => m.GetRecentMessagesAsync(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await _vm.RefreshAsync();

        Assert.AreEqual("Could not load messages: boom", _vm.Status);
        Assert.IsTrue(_vm.StatusIsError);
        Assert.AreEqual("hi", _vm.Messages.Single().Content);
        Assert.AreEqual("conn-1", _vm.Connections.Single().ConnectionId);
    }

    /// <summary>Tests that monitor changes trigger a refresh and that Dispose stops that.</summary>
    [TestMethod]
    public void MonitorChangesTriggerRefreshUntilDisposed()
    {
        int raised = 0;
        _vm.Changed += () => raised++;

        _monitor.Raise(m => m.Changed += null);
        Assert.AreEqual(1, raised);
        _monitor.Verify(m => m.GetRecentMessagesAsync(It.IsAny<int>()), Times.Once);

        // Dispose unsubscribes
        _vm.Dispose();
        _monitor.Raise(m => m.Changed += null);
        Assert.AreEqual(1, raised);
        _monitor.Verify(m => m.GetRecentMessagesAsync(It.IsAny<int>()), Times.Once);
    }

    /// <summary>Tests that refreshing a disposed view model does nothing.</summary>
    [TestMethod]
    public async Task RefreshAsyncDoesNothingAfterDispose()
    {
        int raised = 0;
        _vm.Changed += () => raised++;
        _vm.Dispose();

        await _vm.RefreshAsync();

        _monitor.Verify(m => m.GetRecentMessagesAsync(It.IsAny<int>()), Times.Never);
        Assert.AreEqual(0, raised);
    }

    /// <summary>Tests that a refresh waiting behind a running one is skipped if the view model is disposed meanwhile.</summary>
    [TestMethod]
    public async Task QueuedRefreshIsSkippedWhenDisposedWhileWaiting()
    {
        TaskCompletionSource<IReadOnlyList<Message>> loading = new();
        _monitor.Setup(m => m.GetRecentMessagesAsync(It.IsAny<int>())).Returns(loading.Task);
        int raised = 0;
        _vm.Changed += () => raised++;

        Task running = _vm.RefreshAsync(); // stuck loading messages, holds the gate
        Task queued = _vm.RefreshAsync(); // waits for the gate
        _vm.Dispose();
        loading.SetResult([]);
        await Task.WhenAll(running, queued);

        _monitor.Verify(m => m.GetRecentMessagesAsync(It.IsAny<int>()), Times.Once);
        Assert.AreEqual(1, raised);
    }
}
