// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;
using Cloud.Networking.Mocking;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Test;

/// <summary>
/// Tests that HubConnectionProxy behaves exactly like the HubConnection it wraps.
/// </summary>
[TestClass]
public class TestHubConnectionProxy
{
    private const string Url = "http://localhost/hub";

    private static HubConnection NewHub()
    {
        return new HubConnectionBuilder().WithUrl(Url).Build();
    }

    /// <summary>Runs the action and returns the exception it throws, or null if it completes.</summary>
    private static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>Asserts that both outcomes are the same: both null, or the same exception type and message.</summary>
    private static void AssertSameOutcome(Exception? reference, Exception? actual)
    {
        Assert.AreEqual(reference?.GetType(), actual?.GetType());
        Assert.AreEqual(reference?.Message, actual?.Message);
    }

    /// <summary>Tests that the proxy reports the same state as the wrapped connection, before and after StopAsync.</summary>
    [TestMethod]
    public async Task StateMatchesTheWrappedConnection()
    {
        await using HubConnection hub = NewHub();
        await using HubConnectionProxy proxy = new(hub);

        Assert.AreEqual(hub.State, proxy.State);

        await proxy.StopAsync();

        Assert.AreEqual(hub.State, proxy.State);
    }

    /// <summary>Tests that StartAsync with a cancelled token fails the same way as on a raw connection.</summary>
    [TestMethod]
    public async Task StartAsyncOutcomeMatchesTheRawConnection()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();
        await using HubConnection reference = NewHub();
        await using HubConnectionProxy proxy = new(NewHub());

        Exception? expected = await CaptureAsync(() => reference.StartAsync(cts.Token));
        Exception? actual = await CaptureAsync(() => proxy.StartAsync(cts.Token));

        Assert.IsNotNull(expected);   // guards against comparing null with null
        AssertSameOutcome(expected, actual);
    }

    /// <summary>Tests that StopAsync completes the same way as on a raw connection.</summary>
    [TestMethod]
    public async Task StopAsyncOutcomeMatchesTheRawConnection()
    {
        await using HubConnection reference = NewHub();
        await using HubConnectionProxy proxy = new(NewHub());

        Exception? expected = await CaptureAsync(() => reference.StopAsync());
        Exception? actual = await CaptureAsync(() => proxy.StopAsync());

        AssertSameOutcome(expected, actual);
    }

    /// <summary>Tests that InvokeAsync fails the same way as on a raw connection.</summary>
    [TestMethod]
    public async Task InvokeAsyncOutcomeMatchesTheRawConnection()
    {
        await using HubConnection reference = NewHub();
        await using HubConnectionProxy proxy = new(NewHub());

        Exception? expected = await CaptureAsync(() => reference.InvokeAsync(Contract.SendMessage, "Hello"));
        Exception? actual = await CaptureAsync(() => proxy.InvokeAsync(Contract.SendMessage, "Hello"));

        Assert.IsNotNull(expected);
        AssertSameOutcome(expected, actual);
    }

    /// <summary>Tests that the generic InvokeAsync fails the same way as on a raw connection.</summary>
    [TestMethod]
    public async Task InvokeAsyncWithResultOutcomeMatchesTheRawConnection()
    {
        await using HubConnection reference = NewHub();
        await using HubConnectionProxy proxy = new(NewHub());

        Exception? expected = await CaptureAsync(() => reference.InvokeAsync<string>(Contract.GetMessagesSince, "Hello"));
        Exception? actual = await CaptureAsync(() => proxy.InvokeAsync<string>(Contract.GetMessagesSince, "Hello"));

        Assert.IsNotNull(expected);
        AssertSameOutcome(expected, actual);
    }

    /// <summary>Tests that DisposeAsync leaves the proxy in the same state as a disposed raw connection.</summary>
    [TestMethod]
    public async Task DisposeAsyncLeavesTheSameStateAsTheRawConnection()
    {
        HubConnection reference = NewHub();
        HubConnectionProxy proxy = new(NewHub());

        await reference.DisposeAsync();
        await proxy.DisposeAsync();

        Assert.AreEqual(reference.State, proxy.State);
    }

    /* these are for code coverage as otherwise we need a server */

    /// <summary>Tests that connection events can be subscribed and unsubscribed.</summary>
    [TestMethod]
    public async Task EventsCanBeSubscribedAndUnsubscribed()
    {
        await using HubConnectionProxy proxy = new(NewHub());
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
        await using HubConnectionProxy proxy = new(NewHub());
        IDisposable subscription = proxy.On<Message>(Contract.ReceiveMessage, _ => { });
        Assert.IsNotNull(subscription);
        subscription.Dispose();
    }
}
