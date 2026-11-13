using Cloud.UI.Models;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Test;

[TestClass]
public class NotificationServiceViewModelTests
{
    [TestMethod]
    public void InitialState_IsDisconnectedAndEmpty()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;

        Assert.AreEqual(ConnectionState.Disconnected, vm.State);
        Assert.AreEqual("Disconnected", vm.StateText);
        Assert.AreEqual("Not connected. You won't receive notifications.", vm.StateHint);
        Assert.AreEqual(string.Empty, vm.ServerUrl);
        Assert.AreEqual(string.Empty, vm.MessageText);
        Assert.IsNull(vm.ErrorText);
        Assert.IsFalse(vm.HasError);
        Assert.IsTrue(vm.IsEmpty);
        Assert.AreEqual("Not synced yet", vm.LastSyncText);
    }

    [TestMethod]
    public void InitialCommandAvailability_AllUnavailable()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;

        Assert.IsFalse(vm.ConnectCommand.CanExecute(null));
        Assert.IsFalse(vm.ReconnectCommand.CanExecute(null));
        Assert.IsFalse(vm.DisconnectCommand.CanExecute(null));
        Assert.IsFalse(vm.SendCommand.CanExecute(null));
        Assert.IsFalse(vm.SyncCommand.CanExecute(null));
        Assert.IsFalse(vm.ClearCommand.CanExecute(null));
    }

    [TestMethod]
    public void ServerUrl_Change_EnablesConnect_AndRaisesCanExecuteChanged()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;
        int raised = 0;
        vm.ConnectCommand.CanExecuteChanged += (_, _) => raised++;

        vm.ServerUrl = ViewModelHarness.Url;

        Assert.IsTrue(vm.ConnectCommand.CanExecute(null));
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void MessageText_Change_RaisesSendCanExecuteChanged()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;
        int raised = 0;
        vm.SendCommand.CanExecuteChanged += (_, _) => raised++;

        vm.MessageText = "hi";

        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public async Task Connect_RaisesStatePropertyNotifications()
    {
        ViewModelHarness h = new();
        List<string?> names = new();
        h.ViewModel.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        await h.ConnectAsync();

        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.State));
        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.StateText));
        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.StateHint));
    }

    [TestMethod]
    public async Task Connect_Success_ConnectsAndNormalisesUrl()
    {
        ViewModelHarness h = new();

        await h.ConnectAsync("  http://example.com  ");

        Assert.AreEqual(1, h.Messengers.Count);
        Assert.AreEqual("http://example.com/", h.ViewModel.ServerUrl);
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
        Assert.AreEqual("Connected", h.ViewModel.StateText);
        Assert.AreEqual("Listening for notifications.", h.ViewModel.StateHint);
        Assert.IsFalse(h.ViewModel.HasError);
    }

    [TestMethod]
    public async Task Connect_InvalidUrl_ShowsErrorAndDoesNotCreateMessenger()
    {
        ViewModelHarness h = new();

        await h.ConnectAsync("ftp://nope");

        Assert.AreEqual(0, h.Messengers.Count);
        Assert.IsTrue(h.ViewModel.HasError);
        StringAssert.Contains(h.ViewModel.ErrorText, "http://");
        Assert.AreEqual(ConnectionState.Disconnected, h.ViewModel.State);
    }

    [TestMethod]
    public async Task Connect_Failure_ShowsErrorAndStaysDisconnected()
    {
        NotificationServiceViewModel vm = new(url => TestMessenger.Create(connectError: new IOException("refused")).Object, ViewModelHarness.RunNow) {
            ServerUrl = ViewModelHarness.Url
        };

        await vm.ConnectCommand.ExecuteAsync();

        Assert.AreEqual("refused", vm.ErrorText);
        Assert.IsTrue(vm.HasError);
        Assert.AreEqual(ConnectionState.Disconnected, vm.State);
        Assert.IsTrue(vm.ConnectCommand.CanExecute(null), "the user can retry");
    }

    [TestMethod]
    public async Task Connect_ClearsPreviousError()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync("ftp://nope");
        Assert.IsTrue(h.ViewModel.HasError);

        await h.ConnectAsync();

        Assert.IsNull(h.ViewModel.ErrorText);
    }

    [TestMethod]
    public async Task Connect_WhileRunning_IsBusy_AndAllCommandsUnavailable()
    {
        TaskCompletionSource gate = new();
        Mock<IMessenger>? mock = null;
        NotificationServiceViewModel vm = new(url => (mock = TestMessenger.Create(connectGate: gate.Task)).Object, ViewModelHarness.RunNow) {
            ServerUrl = ViewModelHarness.Url,
            MessageText = "hello"
        };

        Task running = vm.ConnectCommand.ExecuteAsync();

        Assert.AreEqual(ConnectionState.Connecting, vm.State);
        Assert.AreEqual("Connecting…", vm.StateText);
        Assert.IsFalse(vm.ConnectCommand.CanExecute(null));
        Assert.IsFalse(vm.ReconnectCommand.CanExecute(null));
        Assert.IsFalse(vm.DisconnectCommand.CanExecute(null));
        Assert.IsFalse(vm.SendCommand.CanExecute(null));
        Assert.IsFalse(vm.SyncCommand.CanExecute(null));

        gate.SetResult();
        await running;

        Assert.AreEqual(ConnectionState.Connected, vm.State);
        mock!.Verify(m => m.ConnectAsync(), Times.Once);
    }

    [TestMethod]
    public async Task AfterConnect_ReconnectAndDisconnectAvailable_ConnectIsNot()
    {
        ViewModelHarness h = new();

        await h.ConnectAsync();

        Assert.IsFalse(h.ViewModel.ConnectCommand.CanExecute(null));
        Assert.IsTrue(h.ViewModel.ReconnectCommand.CanExecute(null));
        Assert.IsTrue(h.ViewModel.DisconnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Reconnect_ReusesMessenger_AndKeepsNotifications()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.Current.RaiseMessage(TestData.Msg("keep me"));

        await h.ViewModel.ReconnectCommand.ExecuteAsync();

        Assert.AreEqual(1, h.Messengers.Count);
        h.Current.Verify(m => m.DisconnectAsync(), Times.Once);
        h.Current.Verify(m => m.ConnectAsync(), Times.Exactly(2));
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
        Assert.AreEqual(1, h.ViewModel.Notifications.Count);
    }

    [TestMethod]
    public async Task Disconnect_ClosesConnection_AndEnablesConnect()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        await h.ViewModel.DisconnectCommand.ExecuteAsync();

        Assert.AreEqual(ConnectionState.Disconnected, h.ViewModel.State);
        Assert.IsTrue(h.ViewModel.ConnectCommand.CanExecute(null));
        Assert.IsFalse(h.ViewModel.DisconnectCommand.CanExecute(null));
        Assert.IsFalse(h.ViewModel.SendCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Connect_ToDifferentServer_ClearsNotificationsAndDisposesOldMessenger()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync("https://a.example/hub");
        h.Current.RaiseMessage(TestData.Msg("from A"));
        await h.ViewModel.DisconnectCommand.ExecuteAsync();
        Assert.AreEqual(1, h.ViewModel.Notifications.Count);

        await h.ConnectAsync("https://b.example/hub");

        Assert.AreEqual(2, h.Messengers.Count);
        h.Messengers[0].Verify(m => m.DisposeAsync(), Times.Once);
        Assert.IsTrue(h.ViewModel.IsEmpty);
        Assert.AreEqual("Not synced yet", h.ViewModel.LastSyncText);
    }

    [TestMethod]
    public async Task Connect_ToDifferentServer_ResetsDeduplicationAndSyncStart()
    {
        ViewModelHarness h = new();
        Guid id = Guid.NewGuid();
        await h.ConnectAsync("https://a.example/hub");
        h.Current.RaiseMessage(TestData.Msg("x", TestData.T0.AddHours(5), id));
        await h.ViewModel.DisconnectCommand.ExecuteAsync();

        await h.ConnectAsync("https://b.example/hub");
        await h.ViewModel.SyncCommand.ExecuteAsync();
        h.Current.RaiseMessage(TestData.Msg("same id on B", TestData.T0, id));

        h.Current.Verify(m => m.SyncAsync(DateTimeOffset.MinValue), Times.Once);
        Assert.AreEqual(1, h.ViewModel.Notifications.Count);
    }

    [TestMethod]
    public void Send_NotAvailableWhenDisconnected()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;

        vm.MessageText = "hi";

        Assert.IsFalse(vm.SendCommand.CanExecute(null));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task Send_NotAvailableForBlankText(string text)
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.ViewModel.MessageText = text;

        Assert.IsFalse(h.ViewModel.SendCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Send_SendsTrimmedText_AndClearsInput()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.ViewModel.MessageText = "  hello world  ";
        Assert.IsTrue(h.ViewModel.SendCommand.CanExecute(null));

        await h.ViewModel.SendCommand.ExecuteAsync();

        h.Current.Verify(m => m.SendMessageAsync("hello world"), Times.Once);
        Assert.AreEqual(string.Empty, h.ViewModel.MessageText);
        Assert.IsFalse(h.ViewModel.HasError);
    }

    [TestMethod]
    public async Task Send_WhileReconnecting_IsNotAvailable()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.ViewModel.MessageText = "hi";

        h.Current.SetState(ConnectionState.Reconnecting);
        h.Current.RaiseReconnecting();

        Assert.IsFalse(h.ViewModel.SendCommand.CanExecute(null));
    }

    [TestMethod]
    public void Sync_NotAvailableWhenDisconnected()
    {
        Assert.IsFalse(new ViewModelHarness().ViewModel.SyncCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Sync_AddsMissedMessages_NewestOnTop_WithoutAlerts()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        // unordered on purpose
        h.Current.SyncReturns(
            TestData.Msg("second", TestData.T0.AddMinutes(2)),
            TestData.Msg("first", TestData.T0.AddMinutes(1)),
            TestData.Msg("third", TestData.T0.AddMinutes(3)));
        List<NotificationItem> alerts = new();
        h.ViewModel.NotificationReceived += alerts.Add;

        await h.ViewModel.SyncCommand.ExecuteAsync();

        CollectionAssert.AreEqual(
            new[] { "third", "second", "first" },
            h.ViewModel.Notifications.Select(n => n.Content).ToArray());
        Assert.AreEqual(0, alerts.Count);
        StringAssert.EndsWith(h.ViewModel.LastSyncText, "· 3 new");
        StringAssert.StartsWith(h.ViewModel.LastSyncText, "Last synced");
    }

    [TestMethod]
    public async Task Sync_FirstSync_StartsFromMinValue_ThenFromNewestMessage()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.Current.SyncReturns(
            TestData.Msg("a", TestData.T0.AddMinutes(1)),
            TestData.Msg("b", TestData.T0.AddMinutes(9)));

        await h.ViewModel.SyncCommand.ExecuteAsync();
        await h.ViewModel.SyncCommand.ExecuteAsync();

        h.Current.Verify(m => m.SyncAsync(DateTimeOffset.MinValue), Times.Once);
        h.Current.Verify(m => m.SyncAsync(TestData.T0.AddMinutes(9)), Times.Once);
    }

    [TestMethod]
    public async Task Sync_SkipsMessagesAlreadyShown()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        Message live = TestData.Msg("live", TestData.T0);
        h.Current.RaiseMessage(live);
        h.Current.SyncReturns(live, TestData.Msg("missed", TestData.T0.AddMinutes(1)));

        await h.ViewModel.SyncCommand.ExecuteAsync();

        Assert.AreEqual(2, h.ViewModel.Notifications.Count);
        StringAssert.EndsWith(h.ViewModel.LastSyncText, "· 1 new");
    }

    [TestMethod]
    public async Task Sync_NothingMissed_Reports0New()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        await h.ViewModel.SyncCommand.ExecuteAsync();

        StringAssert.EndsWith(h.ViewModel.LastSyncText, "· 0 new");
        Assert.IsTrue(h.ViewModel.IsEmpty);
    }

    [TestMethod]
    public async Task MessageReceived_AddsItemOnTop_AndRaisesNotificationReceived()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        List<NotificationItem> alerts = new();
        h.ViewModel.NotificationReceived += alerts.Add;
        Message first = TestData.Msg("one", TestData.T0, sender: "alice");
        Message second = TestData.Msg("two", TestData.T0.AddMinutes(1), sender: "bob");

        h.Current.RaiseMessage(first);
        h.Current.RaiseMessage(second);

        Assert.AreEqual(2, h.ViewModel.Notifications.Count);
        Assert.AreEqual("two", h.ViewModel.Notifications[0].Content);
        Assert.AreEqual("bob", h.ViewModel.Notifications[0].SenderId);
        Assert.AreEqual(second.Id, h.ViewModel.Notifications[0].Id);
        Assert.AreEqual(second.Timestamp, h.ViewModel.Notifications[0].ReceivedAt);
        Assert.AreEqual(2, alerts.Count);
        Assert.AreSame(h.ViewModel.Notifications[0], alerts[1]);
    }

    [TestMethod]
    public async Task MessageReceived_Duplicate_IsIgnored()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        int alerts = 0;
        h.ViewModel.NotificationReceived += _ => alerts++;
        Message m = TestData.Msg();

        h.Current.RaiseMessage(m);
        h.Current.RaiseMessage(m);

        Assert.AreEqual(1, h.ViewModel.Notifications.Count);
        Assert.AreEqual(1, alerts);
    }

    [TestMethod]
    public async Task Notifications_Change_RaisesIsEmpty_AndUpdatesClearAvailability()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        List<string?> names = new();
        h.ViewModel.PropertyChanged += (_, e) => names.Add(e.PropertyName);
        int canExecuteChanged = 0;
        h.ViewModel.ClearCommand.CanExecuteChanged += (_, _) => canExecuteChanged++;

        h.Current.RaiseMessage(TestData.Msg());

        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.IsEmpty));
        Assert.AreEqual(1, canExecuteChanged);
        Assert.IsFalse(h.ViewModel.IsEmpty);
        Assert.IsTrue(h.ViewModel.ClearCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Clear_EmptiesList()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.Current.RaiseMessage(TestData.Msg("a"));
        h.Current.RaiseMessage(TestData.Msg("b"));

        h.ViewModel.ClearCommand.Execute(null);

        Assert.IsTrue(h.ViewModel.IsEmpty);
        Assert.IsFalse(h.ViewModel.ClearCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Reconnecting_UpdatesState()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.Current.SetState(ConnectionState.Reconnecting);
        h.Current.RaiseReconnecting(new IOException("lost"));

        Assert.AreEqual(ConnectionState.Reconnecting, h.ViewModel.State);
        Assert.AreEqual("Reconnecting…", h.ViewModel.StateText);
        Assert.AreEqual("Connection lost. Trying to reconnect…", h.ViewModel.StateHint);
        Assert.IsTrue(h.ViewModel.ReconnectCommand.CanExecute(null));
        Assert.IsTrue(h.ViewModel.DisconnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Reconnected_ClearsErrorAndUpdatesState()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.Current.SetState(ConnectionState.Disconnected);
        h.Current.RaiseClosed(new IOException("boom"));
        Assert.IsTrue(h.ViewModel.HasError);

        h.Current.SetState(ConnectionState.Connected);
        h.Current.RaiseReconnected("id");

        Assert.IsNull(h.ViewModel.ErrorText);
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
    }

    [TestMethod]
    public async Task Closed_WithException_ShowsErrorAndDisconnects()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.Current.SetState(ConnectionState.Disconnected);
        h.Current.RaiseClosed(new IOException("boom"));

        Assert.AreEqual("Connection closed: boom", h.ViewModel.ErrorText);
        Assert.AreEqual(ConnectionState.Disconnected, h.ViewModel.State);
        Assert.IsTrue(h.ViewModel.ConnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Closed_WithoutException_DisconnectsWithoutError()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.Current.SetState(ConnectionState.Disconnected);
        h.Current.RaiseClosed(null);

        Assert.IsFalse(h.ViewModel.HasError);
        Assert.AreEqual(ConnectionState.Disconnected, h.ViewModel.State);
    }

    [TestMethod]
    public async Task DisposeAsync_DisposesMessenger()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        await h.ViewModel.DisposeAsync();

        h.Current.Verify(m => m.DisposeAsync(), Times.Once);
    }
}
