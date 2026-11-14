// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;

namespace Cloud.Networking.Server.Test.Models;

[TestClass]
public class TestInMemoryMessageStore
{
    private static Message NewMessage(string content)
    {
        return new() { SenderId = "sender-1", Content = content };
    }

    /// <summary>Tests that the cap must be positive and defaults to DefaultMaxMessages.</summary>
    [TestMethod]
    public void MaxMessagesMustBePositiveAndHasADefault()
    {
        Assert.AreEqual(InMemoryMessageStore.DefaultMaxMessages, new InMemoryMessageStore().MaxMessages);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new InMemoryMessageStore(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new InMemoryMessageStore(-1));
    }

    /// <summary>Tests that a null message is rejected.</summary>
    [TestMethod]
    public void NullMessageIsRejected()
    {
        InMemoryMessageStore store = new();
        Assert.ThrowsExactly<ArgumentNullException>(() => { _ = store.AddAsync(null!); });
    }

    /// <summary>Tests that recent messages are returned newest first and limited to the requested count.</summary>
    [TestMethod]
    public async Task RecentMessagesAreReturnedNewestFirstAndLimitedToCount()
    {
        InMemoryMessageStore store = new();
        Message first = NewMessage("first");
        Message second = NewMessage("second");
        Message third = NewMessage("third");
        await store.AddAsync(first);
        await store.AddAsync(second);
        await store.AddAsync(third);

        CollectionAssert.AreEqual(new[] { third, second }, (await store.GetRecentAsync(2)).ToArray());
        CollectionAssert.AreEqual(new[] { third, second, first }, (await store.GetRecentAsync(10)).ToArray());
    }

    /// <summary>Tests that a negative count is rejected.</summary>
    [TestMethod]
    public void NegativeRecentCountIsRejected()
    {
        InMemoryMessageStore store = new();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = store.GetRecentAsync(-1); });
    }

    /// <summary>Tests that the oldest message is dropped once the cap is exceeded.</summary>
    [TestMethod]
    public async Task OldestMessageIsDroppedWhenCapIsExceeded()
    {
        InMemoryMessageStore store = new(maxMessages: 2);
        Message oldest = NewMessage("oldest");
        Message middle = NewMessage("middle");
        Message newest = NewMessage("newest");

        await store.AddAsync(oldest);
        await store.AddAsync(middle);
        await store.AddAsync(newest);

        CollectionAssert.AreEqual(new[] { newest, middle }, (await store.GetRecentAsync(10)).ToArray());
    }

    /// <summary>Tests that MessageAdded is raised with the added message.</summary>
    [TestMethod]
    public async Task MessageAddedIsRaisedWithTheAddedMessage()
    {
        InMemoryMessageStore store = new();
        Message? raisedWith = null;
        store.MessageAdded += m => raisedWith = m;
        Message message = NewMessage("hello");

        await store.AddAsync(message);

        Assert.AreSame(message, raisedWith);
    }

    /// <summary>Tests that only strictly newer messages are returned, oldest first, regardless of the order they were added in.</summary>
    [TestMethod]
    public async Task MessagesSinceAreNewerThanTheGivenTimeAndOldestFirst()
    {
        DateTimeOffset since = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        Message newest = new() { SenderId = "sender-1", Content = "newest", Timestamp = since.AddSeconds(2) };
        Message atSince = new() { SenderId = "sender-1", Content = "at since", Timestamp = since };
        Message newer = new() { SenderId = "sender-1", Content = "newer", Timestamp = since.AddSeconds(1) };

        InMemoryMessageStore store = new();
        await store.AddAsync(newest);
        await store.AddAsync(atSince);
        await store.AddAsync(newer);

        CollectionAssert.AreEqual(new[] { newer, newest }, (await store.GetSinceAsync(since)).ToArray());
    }
}
