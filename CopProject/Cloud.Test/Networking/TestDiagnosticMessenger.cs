// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;
using Cloud.Networking.Decorators;
using Cloud.Networking.Mocking;
using Moq;

namespace Cloud.Test;

/// <summary>
/// Tests the DiagnosticMessenger class
/// </summary>
[TestClass]
public class TestDiagnosticMessenger
{
    private Mock<IMessenger> _inner = null!;
    private Mock<ITracer> _tracer = null!;

    /* every line written to the tracer, in order */
    private List<string> _lines = null!;

    [TestInitialize]
    public void Init()
    {
        _inner = new Mock<IMessenger>();
        _tracer = new Mock<ITracer>();
        _lines = new List<string>();

        _tracer.Setup(t => t.WriteLine(It.IsAny<string>()))
            .Callback<string>(line => _lines.Add(line));

        IReadOnlyList<Message> none = Array.Empty<Message>();
        _inner.Setup(m => m.SyncAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(none);
    }

    /// <summary>Creates a DiagnosticMessenger over the mocked messenger and tracer.</summary>
    private DiagnosticMessenger CreateMessenger()
    {
        return new(_inner.Object, _tracer.Object);
    }

    /// <summary>Creates a test message.</summary>
    private static Message NewMessage(string content = "hello")
    {
        return new() { SenderId = "sender-123", Content = content };
    }

    /* constructor */
    /// <summary>Tests that the constructor rejects a null messenger and a null tracer.</summary>
    [TestMethod]
    public void ConstructorRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new DiagnosticMessenger(null!, _tracer.Object));
        Assert.Throws<ArgumentNullException>(() => new DiagnosticMessenger(_inner.Object, null!));
    }

    /* state */
    /// <summary>Tests that the state is taken from the decorated messenger.</summary>
    [TestMethod]
    public void StateReflectsTheInnerState()
    {
        foreach (ConnectionState state in Enum.GetValues<ConnectionState>())
        {
            _inner.SetupGet(m => m.State).Returns(state);
            Assert.AreEqual(state, CreateMessenger().State);
        }
    }

    /* ConnectAsync */
    /// <summary>Tests that ConnectAsync forwards the call and traces before and after it.</summary>
    [TestMethod]
    public async Task ConnectAsyncForwardsAndTraces()
    {
        await CreateMessenger().ConnectAsync();

        _inner.Verify(m => m.ConnectAsync(), Times.Once);
        CollectionAssert.AreEqual(new[] { "-- connecting", "-- connected" }, _lines);
    }

    /// <summary>Tests that ConnectAsync traces a failure and rethrows the same exception.</summary>
    [TestMethod]
    public async Task ConnectAsyncTracesAndRethrowsFailure()
    {
        InvalidOperationException error = new("boom");
        _inner.Setup(m => m.ConnectAsync()).ThrowsAsync(error);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMessenger().ConnectAsync());

        Assert.AreSame(error, thrown);
        Assert.AreEqual(2, _lines.Count);
        Assert.AreEqual("-- connecting", _lines[0]);
        Assert.IsTrue(_lines[1].StartsWith("-- connection failed: "));
        Assert.IsTrue(_lines[1].Contains("boom"));
    }

    /* SendMessageAsync */
    /// <summary>Tests that SendMessageAsync forwards the content and traces the success.</summary>
    [TestMethod]
    public async Task SendMessageAsyncForwardsAndTraces()
    {
        await CreateMessenger().SendMessageAsync("hello");

        _inner.Verify(m => m.SendMessageAsync("hello"), Times.Once);
        CollectionAssert.AreEqual(new[] { "-- message sent" }, _lines);
    }

    /// <summary>Tests that SendMessageAsync traces a failure and rethrows the same exception.</summary>
    [TestMethod]
    public async Task SendMessageAsyncTracesAndRethrowsFailure()
    {
        InvalidOperationException error = new("boom");
        _inner.Setup(m => m.SendMessageAsync(It.IsAny<string>())).ThrowsAsync(error);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMessenger().SendMessageAsync("hello"));

        Assert.AreSame(error, thrown);
        Assert.AreEqual(1, _lines.Count);
        Assert.IsTrue(_lines[0].StartsWith("-- message sending failed: "));
    }

    /* SyncAsync */
    /// <summary>Tests that SyncAsync returns the inner result and traces how many messages were synced.</summary>
    [TestMethod]
    public async Task SyncAsyncReturnsTheMessagesAndTraces()
    {
        DateTimeOffset since = DateTimeOffset.UtcNow;
        IReadOnlyList<Message> missed = [NewMessage("a"), NewMessage("b")];
        _inner.Setup(m => m.SyncAsync(since)).ReturnsAsync(missed);

        IReadOnlyList<Message> result = await CreateMessenger().SyncAsync(since);

        Assert.AreSame(missed, result);
        CollectionAssert.AreEqual(new[] { "-- synced 2 message(s)" }, _lines);
    }

    /// <summary>Tests that SyncAsync traces a failure and rethrows the same exception.</summary>
    [TestMethod]
    public async Task SyncAsyncTracesAndRethrowsFailure()
    {
        InvalidOperationException error = new("boom");
        _inner.Setup(m => m.SyncAsync(It.IsAny<DateTimeOffset>())).ThrowsAsync(error);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMessenger().SyncAsync(DateTimeOffset.MinValue));

        Assert.AreSame(error, thrown);
        Assert.AreEqual(1, _lines.Count);
        Assert.IsTrue(_lines[0].StartsWith("-- sync failed: "));
    }

    /* DisconnectAsync */
    /// <summary>Tests that DisconnectAsync forwards the call and traces the success.</summary>
    [TestMethod]
    public async Task DisconnectAsyncForwardsAndTraces()
    {
        await CreateMessenger().DisconnectAsync();

        _inner.Verify(m => m.DisconnectAsync(), Times.Once);
        CollectionAssert.AreEqual(new[] { "-- disconnected" }, _lines);
    }

    /// <summary>Tests that DisconnectAsync traces a failure and rethrows the same exception.</summary>
    [TestMethod]
    public async Task DisconnectAsyncTracesAndRethrowsFailure()
    {
        InvalidOperationException error = new("boom");
        _inner.Setup(m => m.DisconnectAsync()).ThrowsAsync(error);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMessenger().DisconnectAsync());

        Assert.AreSame(error, thrown);
        Assert.AreEqual(1, _lines.Count);
        Assert.IsTrue(_lines[0].StartsWith("-- disconnection failed: "));
    }

    /* events */
    /// <summary>Tests that every inner event is traced and forwarded to subscribers.</summary>
    [TestMethod]
    public void AllEventsAreTracedAndForwarded()
    {
        DiagnosticMessenger messenger = CreateMessenger();
        Message? receivedMessage = null;
        Exception? reconnecting = null;
        string? reconnectedId = null;
        Exception? closed = null;
        messenger.MessageReceived += m => receivedMessage = m;
        messenger.Reconnecting += e => reconnecting = e;
        messenger.Reconnected += id => reconnectedId = id;
        messenger.Closed += e => closed = e;
        Message sent = NewMessage();
        InvalidOperationException error = new("boom");

        _inner.Raise(m => m.MessageReceived += null!, sent);
        _inner.Raise(m => m.Reconnecting += null!, error);
        _inner.Raise(m => m.Reconnected += null!, "new-id");
        _inner.Raise(m => m.Closed += null!, error);

        Assert.AreSame(sent, receivedMessage);
        Assert.AreSame(error, reconnecting);
        Assert.AreEqual("new-id", reconnectedId);
        Assert.AreSame(error, closed);
        CollectionAssert.AreEqual(
            new[] {
                "-- message received from sender-123",
                "-- reconnecting: boom",
                "-- reconnected: new-id",
                "-- connection closed: boom"
            },
            _lines);
    }

    /// <summary>Tests that events with no subscribers and no exception are still traced and do not throw.</summary>
    [TestMethod]
    public void EventsWithoutSubscribersOrExceptionAreStillTraced()
    {
        _ = CreateMessenger();

        _inner.Raise(m => m.MessageReceived += null!, NewMessage());
        _inner.Raise(m => m.Reconnecting += null!, (Exception)null!);
        _inner.Raise(m => m.Reconnected += null!, "new-id");
        _inner.Raise(m => m.Closed += null!, (Exception)null!);

        CollectionAssert.AreEqual(
            new[] {
                "-- message received from sender-123",
                "-- reconnecting: ",
                "-- reconnected: new-id",
                "-- connection closed: "
            },
            _lines);
    }

    /* DisposeAsync */
    /// <summary>Tests that DisposeAsync disposes the decorated messenger and stops reacting to its events.</summary>
    [TestMethod]
    public async Task DisposeAsyncDisposesAndUnsubscribesFromTheInnerEvents()
    {
        DiagnosticMessenger messenger = CreateMessenger();
        int calls = 0;
        messenger.MessageReceived += _ => calls++;
        messenger.Reconnecting += _ => calls++;
        messenger.Reconnected += _ => calls++;
        messenger.Closed += _ => calls++;

        await messenger.DisposeAsync();

        _inner.Raise(m => m.MessageReceived += null!, NewMessage());
        _inner.Raise(m => m.Reconnecting += null!, (Exception)null!);
        _inner.Raise(m => m.Reconnected += null!, "id");
        _inner.Raise(m => m.Closed += null!, (Exception)null!);

        _inner.Verify(m => m.DisposeAsync(), Times.Once);
        Assert.AreEqual(0, calls);
        Assert.AreEqual(0, _lines.Count);
    }

    /// <summary>Tests that DisposeAsync traces a failure and rethrows the same exception.</summary>
    [TestMethod]
    public async Task DisposeAsyncTracesAndRethrowsFailure()
    {
        InvalidOperationException error = new("boom");
        _inner.Setup(m => m.DisposeAsync()).Returns(ValueTask.FromException(error));

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMessenger().DisposeAsync().AsTask());

        Assert.AreSame(error, thrown);
        Assert.AreEqual(1, _lines.Count);
        Assert.IsTrue(_lines[0].StartsWith("-- disposal failed: "));
    }
}
