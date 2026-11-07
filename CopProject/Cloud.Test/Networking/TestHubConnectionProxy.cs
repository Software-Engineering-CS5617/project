// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Test;

/// <summary>
/// Tests the HubConnectionProxy class
/// </summary>
[TestClass]
public class TestHubConnectionProxy
{
    // Creates a HubConnectionProxy with a test URL.
    private static HubConnectionProxy CreateProxy()
    {
        return new(new HubConnectionBuilder().WithUrl("http://localhost/hub").Build());
    }

    /// <summary>Tests that the initial connection state is disconnected.</summary>
    [TestMethod]
    public async Task StateIsDisconnectedInitially()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        Assert.AreEqual(HubConnectionState.Disconnected, proxy.State);
    }

    /// <summary>Tests that connection events support subscription and unsubscription.</summary>
    [TestMethod]
    public async Task EventsCanBeSubscribedAndUnsubscribed()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        Func<Exception?, Task> exceptionHandler = _ => Task.CompletedTask;
        Func<string?, Task> idHandler = _ => Task.CompletedTask;

        proxy.Reconnecting += exceptionHandler;
        proxy.Reconnecting -= exceptionHandler;
        proxy.Reconnected += idHandler;
        proxy.Reconnected -= idHandler;
        proxy.Closed += exceptionHandler;
        proxy.Closed -= exceptionHandler;
    }

    /// <summary>Tests that registering a message handler returns a disposable subscription.</summary>
    [TestMethod]
    public async Task OnReturnsADisposable()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        IDisposable dis = proxy.On<Message>(Contract.ReceiveMessage, _ => { });
        Assert.IsNotNull(dis);
        dis.Dispose();
    }

    /// <summary>Tests that starting the connection with a cancelled token throws an exception.</summary>
    [TestMethod]
    public async Task StartAsyncOnCancellingTheTokenThrows()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        using CancellationTokenSource cts = new();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => proxy.StartAsync(cts.Token));
    }

    /// <summary>Tests that stopping an already disconnected connection completes successfully.</summary>
    [TestMethod]
    public async Task StopAsyncOnDisconnectedStateCompletes()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        await proxy.StopAsync();
        Assert.AreEqual(HubConnectionState.Disconnected, proxy.State);
    }

    /// <summary>Tests that disposing an already disconnected connection completes successfully.</summary>
    [TestMethod]
    public async Task DisposeAsyncOnDisconnectedStateCompletes()
    {
        HubConnectionProxy proxy = CreateProxy();
        await proxy.DisposeAsync();
        Assert.AreEqual(HubConnectionState.Disconnected, proxy.State);
    }

    /// <summary>Tests that invoking a server method while disconnected throws an exception.</summary>
    [TestMethod]
    public async Task InvokeAsyncOnDisconnectedStateThrows()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.InvokeAsync(Contract.SendMessage, "Hello"));
    }
}
