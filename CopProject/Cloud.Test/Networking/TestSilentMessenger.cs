// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;
using Cloud.Networking.Decorators;
using Moq;

namespace Cloud.Test;

/// <summary>
/// Tests the SilentMessenger class
/// </summary>
[TestClass]
public class TestSilentMessenger
{
    private Mock<IMessenger> _inner = null!;

    [TestInitialize]
    public void Init()
    {
        _inner = new Mock<IMessenger>();

        IReadOnlyList<Message> none = Array.Empty<Message>();
        _inner.Setup(m => m.SyncAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(none);
    }

    /// <summary>Creates a SilentMessenger over the mocked messenger.</summary>
    private SilentMessenger CreateMessenger()
    {
        return new(_inner.Object);
    }

    /// <summary>Creates a test message.</summary>
    private static Message NewMessage(string content = "hello")
    {
        return new() { SenderId = "sender-123", Content = content };
    }

    /* constructor */
    /// <summary>Tests that the constructor rejects a null messenger.</summary>
    [TestMethod]
    public void ConstructorRejectsNullMessenger()
    {
        Assert.Throws<ArgumentNullException>(() => new SilentMessenger(null!));
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
    /// <summary>Tests that ConnectAsync forwards the call to the decorated messenger.</summary>
    [TestMethod]
    public async Task ConnectAsyncForwards()
    {
        await CreateMessenger().ConnectAsync();
        _inner.Verify(m => m.ConnectAsync(), Times.Once);
    }

    /// <summary>Tests that ConnectAsync calls the decorated messenger and swallows its failure.</summary>
    [TestMethod]
    public async Task ConnectAsyncSwallowsFailure()
    {
        _inner.Setup(m => m.ConnectAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        await CreateMessenger().ConnectAsync();

        _inner.Verify(m => m.ConnectAsync(), Times.Once);
    }

    /* SendMessageAsync */
    /// <summary>Tests that SendMessageAsync forwards the content to the decorated messenger.</summary>
    [TestMethod]
    public async Task SendMessageAsyncForwards()
    {
        await CreateMessenger().SendMessageAsync("hello");
        _inner.Verify(m => m.SendMessageAsync("hello"), Times.Once);
    }

    /// <summary>Tests that SendMessageAsync forwards the content and swallows failures.</summary>
    [TestMethod]
    public async Task SendMessageAsyncSwallowsFailure()
    {
        _inner.Setup(m => m.SendMessageAsync(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("boom"));

        await CreateMessenger().SendMessageAsync("hello");

        _inner.Verify(m => m.SendMessageAsync("hello"), Times.Once);
    }

    /* SyncAsync */
    /// <summary>Tests that SyncAsync returns what the decorated messenger returns.</summary>
    [TestMethod]
    public async Task SyncAsyncReturnsTheMessages()
    {
        DateTimeOffset since = DateTimeOffset.UtcNow;
        IReadOnlyList<Message> missed = [NewMessage("a"), NewMessage("b")];
        _inner.Setup(m => m.SyncAsync(since)).ReturnsAsync(missed);

        IReadOnlyList<Message> result = await CreateMessenger().SyncAsync(since);

        Assert.AreSame(missed, result);
    }

    /// <summary>Tests that SyncAsync swallows failures and returns an empty list.</summary>
    [TestMethod]
    public async Task SyncAsyncReturnsEmptyOnFailure()
    {
        _inner.Setup(m => m.SyncAsync(It.IsAny<DateTimeOffset>())).ThrowsAsync(new InvalidOperationException("boom"));

        IReadOnlyList<Message> result = await CreateMessenger().SyncAsync(DateTimeOffset.MinValue);

        Assert.AreEqual(0, result.Count);
    }

    /* DisconnectAsync */
    /// <summary>Tests that DisconnectAsync forwards the call to the decorated messenger.</summary>
    [TestMethod]
    public async Task DisconnectAsyncForwards()
    {
        await CreateMessenger().DisconnectAsync();
        _inner.Verify(m => m.DisconnectAsync(), Times.Once);
    }

    /// <summary>Tests that DisconnectAsync calls the decorated messenger and swallows its failure.</summary>
    [TestMethod]
    public async Task DisconnectAsyncSwallowsFailure()
    {
        _inner.Setup(m => m.DisconnectAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        await CreateMessenger().DisconnectAsync();

        _inner.Verify(m => m.DisconnectAsync(), Times.Once);
    }

    /* events */
    /// <summary>Tests that all inner events are forwarded to subscribers.</summary>
    [TestMethod]
    public void AllEventsAreForwarded()
    {
        SilentMessenger messenger = CreateMessenger();
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
    }

    /// <summary>Tests that events raised while nobody is subscribed do not throw.</summary>
    [TestMethod]
    public void EventsWithoutSubscribersDoNotThrow()
    {
        _ = CreateMessenger();

        // nothing is subscribed to the decorator: raising must simply do nothing
        _inner.Raise(m => m.MessageReceived += null!, NewMessage());
        _inner.Raise(m => m.Reconnecting += null!, new InvalidOperationException("boom"));
        _inner.Raise(m => m.Reconnected += null!, "new-id");
        _inner.Raise(m => m.Closed += null!, new InvalidOperationException("boom"));
    }

    /* DisposeAsync */
    /// <summary>Tests that DisposeAsync disposes the decorated messenger and stops forwarding its events.</summary>
    [TestMethod]
    public async Task DisposeAsyncDisposesAndUnsubscribesFromTheInnerEvents()
    {
        SilentMessenger messenger = CreateMessenger();
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
    }

    /// <summary>Tests that DisposeAsync swallows failures.</summary>
    [TestMethod]
    public async Task DisposeAsyncSwallowsFailure()
    {
        _inner.Setup(m => m.DisposeAsync()).Returns(ValueTask.FromException(new InvalidOperationException("boom")));

        await CreateMessenger().DisposeAsync();

        _inner.Verify(m => m.DisposeAsync(), Times.Once);
    }
}
