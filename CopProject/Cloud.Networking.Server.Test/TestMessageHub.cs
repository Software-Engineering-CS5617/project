// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Cloud.Networking.Server.Test;

[TestClass]
public class TestMessageHub
{
    private readonly Mock<IClientProxy> _all = new();
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

        _hub = new MessageHub { Clients = clients.Object, Context = context.Object };
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
        // rejection before SendCoreAsync is called.
        await Assert.ThrowsAsync<HubException>(() => _hub.SendMessage(content));
        _all.Verify(a => a.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SendMessageBroadcastsTrimmedContentFromTheCallersConnectionId()
    {
        object?[]? sentArgs = null;

        // intercept the call and store the args in sentArgs
        _all.Setup(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => sentArgs = args)
            .Returns(Task.CompletedTask);

        await _hub.SendMessage("  hello  ");
        _all.Verify(a => a.SendCoreAsync(Contract.ReceiveMessage, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);

        Message message = (Message)sentArgs!.Single()!;
        Assert.AreEqual("hello", message.Content);
        Assert.AreEqual("conn-1", message.SenderId);
    }
}
