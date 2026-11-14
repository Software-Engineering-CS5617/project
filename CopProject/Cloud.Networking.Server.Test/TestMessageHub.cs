// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Cloud.Networking.Server.Test;

[TestClass]
public class TestMessageHub
{
    private readonly Mock<IClientProxy> _all = new();
    private readonly Mock<IMessageStore> _store = new();
    private MessageHub _hub = null!;

    /// <summary>
    /// <see cref="Hub"/> for us clients and context are the only things relevant.
    /// hardcode some relevant behaviours at the start
    /// </summary>
    [TestInitialize]
    public void Init()
    {
        Mock<IHubCallerClients> clients = new();
        clients.SetupGet(c => c.All).Returns(_all.Object);

        Mock<HubCallerContext> context = new();
        context.SetupGet(c => c.ConnectionId).Returns("conn-1");

        _hub = new MessageHub(_store.Object) { Clients = clients.Object, Context = context.Object };
    }

    private void VerifyNothingWasStoredOrBroadcast()
    {
        _store.Verify(s => s.AddAsync(It.IsAny<Message>()), Times.Never);
        _all.Verify(a => a.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Tests that the hub cannot be created without a message store.</summary>
    [TestMethod]
    public void NullStoreIsRejected()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new MessageHub(null!));
    }

    /* SendMessage */
    /// <summary>
    /// <see cref="ClientProxyExtensions" /> 
    /// _hub.SendMessage calls SendAsync calls SendCoreAsync 
    /// </summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task SendMessageRejectsBlankContent(string content)
    {
        // rejection before the store or SendCoreAsync is touched.
        await Assert.ThrowsAsync<HubException>(() => _hub.SendMessage(content));
        VerifyNothingWasStoredOrBroadcast();
    }

    /// <summary>Tests that content longer than the limit is rejected.</summary>
    [TestMethod]
    public async Task SendMessageRejectsTooLongContent()
    {
        await Assert.ThrowsAsync<HubException>(() => _hub.SendMessage(new string('a', MessageHub.MaxContentLength + 1)));
        VerifyNothingWasStoredOrBroadcast();
    }

    /// <summary>Tests that the trimmed message is stored first, then broadcast and returned with the caller's connection ID.</summary>
    [TestMethod]
    public async Task SendMessageStoresThenBroadcastsTrimmedContentFromTheCallersConnectionId()
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

        // intercept the call and store the args in sentArgs
        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => {
                order.Add("broadcast");
                sentArgs = args;
            })
            .Returns(Task.CompletedTask);

        Message returned = await _hub.SendMessage("  hello  ");

        CollectionAssert.AreEqual(new[] { "store", "broadcast" }, order);
        Message broadcast = (Message)sentArgs!.Single()!;
        Assert.AreSame(stored, broadcast);
        Assert.AreSame(broadcast, returned);
        Assert.AreEqual("hello", returned.Content);
        Assert.AreEqual("conn-1", returned.SenderId);
    }

    /* GetMessagesSince */
    /// <summary>Tests that the messages newer than the given time come straight from the store.</summary>
    [TestMethod]
    public async Task GetMessagesSinceReturnsWhatTheStoreHasSince()
    {
        DateTimeOffset since = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        Message[] expected = [new() { SenderId = "client-1", Content = "hi" }];
        _store.Setup(s => s.GetSinceAsync(since)).ReturnsAsync(expected);

        Message[] result = await _hub.GetMessagesSince(since);

        CollectionAssert.AreEqual(expected, result);
    }
}
