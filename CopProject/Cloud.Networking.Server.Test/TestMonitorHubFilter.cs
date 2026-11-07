// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Cloud.Networking.Server.Test;

/// <summary>
/// Tests the MonitorHub filter.
/// </summary>
/// <remarks>
/// Rationales on Mocking choices:
/// it is obvious on why we should mock server monitor
///     <see cref="HubInvocationContext" /> & <see cref="HubLifetimeContext"/> 
///     contains <see cref="HubCallerContext"/>, <see cref="IServiceProvider"/>, <see cref="Hub"/>
/// as we have hub lets fix it and use the real one and lets fix the hub method and the params
/// Rest two we can mock as one is abstract and other is an interface
/// so we will do that itself.
/// </remarks>
[TestClass]
public class TestMonitorHubFilter
{
    private Mock<IServerMonitor> _monitor = null!;
    private Mock<HubCallerContext> _caller = null!;
    private Mock<IServiceProvider> _services = null!;
    private MonitorHubFilter _filter = null!;
    private Hub _hub = null!; /* using the real hub is easier */

    /// <summary>Initializes the dependencies used by the tests.</summary>
    [TestInitialize]
    public void Init()
    {
        _monitor = new Mock<IServerMonitor>();
        _caller = new Mock<HubCallerContext>();
        _caller.SetupGet(c => c.ConnectionId).Returns("conn-1");
        _services = new Mock<IServiceProvider>();
        _hub = new MessageHub();

        _filter = new MonitorHubFilter(_monitor.Object);
    }

    private HubInvocationContext CreateInvocation()
    {
        return new(
            _caller.Object,
            _services.Object,
            _hub,
            typeof(MessageHub).GetMethod(nameof(MessageHub.SendMessage))!,
            ["hello"]);
    }

    private HubLifetimeContext CreateLifetime()
    {
        return new(
            _caller.Object,
            _services.Object,
            _hub
        );
    }

    /// <summary>Tests that a returned message is recorded and passed back unchanged.</summary>
    [TestMethod]
    public async Task InvokeMethodRecordsReturnedMessage()
    {
        Message message = new() { SenderId = "conn-1", Content = "hello" };

        object? result = await _filter.InvokeMethodAsync(
            CreateInvocation(),
            _ => new ValueTask<object?>(message));

        Assert.AreSame(message, result);
        _monitor.Verify(m => m.RecordMessage(message), Times.Once);
    }

    /// <summary>Tests that results other than messages are not recorded.</summary>
    [TestMethod]
    public async Task InvokeMethodIgnoresNonMessageResults()
    {
        object? result = await _filter.InvokeMethodAsync(
            CreateInvocation(),
            _ => new ValueTask<object?>("not a message"));

        Assert.AreEqual("not a message", result);
        _monitor.Verify(m => m.RecordMessage(It.IsAny<Message>()), Times.Never);
    }

    /// <summary>Tests that a message is not recorded when the hub method throws.</summary>
    [TestMethod]
    public async Task InvokeMethodDoesNotRecordWhenNextThrows()
    {
        await Assert.ThrowsAsync<HubException>(
            () => _filter.InvokeMethodAsync(
                CreateInvocation(),
                _ => throw new HubException("fail")).AsTask());

        _monitor.Verify(m => m.RecordMessage(It.IsAny<Message>()), Times.Never);
    }

    /// <summary>Tests that a connection is registered after the connection pipeline succeeds.</summary>
    [TestMethod]
    public async Task OnConnectedRegistersConnection()
    {
        bool registered = false;

        _monitor.Setup(m => m.Connected("conn-1"))
            .Callback(() => registered = true);

        await _filter.OnConnectedAsync(
            CreateLifetime(),
            _ => {
                Assert.IsFalse(registered);
                return Task.CompletedTask;
            });

        Assert.IsTrue(registered);
        _monitor.Verify(m => m.Connected("conn-1"), Times.Once);
    }

    /// <summary>Tests that a connection is not registered when the connection pipeline throws.</summary>
    [TestMethod]
    public async Task OnConnectedDoesNotRegisterWhenNextThrows()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _filter.OnConnectedAsync(
                CreateLifetime(),
                _ => throw new InvalidOperationException("fail")));

        _monitor.Verify(m => m.Connected(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Tests that a connection is removed when the client disconnects.</summary>
    [TestMethod]
    public async Task OnDisconnectedRemovesConnection()
    {
        await _filter.OnDisconnectedAsync(
            CreateLifetime(),
            null,
            (_, _) => Task.CompletedTask);

        _monitor.Verify(m => m.Disconnected("conn-1"), Times.Once);
    }

    /// <summary>Tests that a connection is removed even when the disconnect pipeline throws.</summary>
    [TestMethod]
    public async Task OnDisconnectedRemovesConnectionWhenNextThrows()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _filter.OnDisconnectedAsync(
                CreateLifetime(),
                null,
                (_, _) => throw new InvalidOperationException("fail")));

        _monitor.Verify(m => m.Disconnected("conn-1"), Times.Once);
    }
}
