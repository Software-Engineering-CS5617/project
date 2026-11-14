// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR;

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Keeps the Monitor's connection list in step with the Hub's connection lifecycle.
/// Client -> MessageHub -> MonitorHubFilter -> Monitor (as it raises changed event)
/// Monitor -> ViewModel -> View
/// Messages do not go through here: the Monitor hears about them from the message store.
/// </summary>
/// 
/// <remarks>
/// Reference: https://learn.microsoft.com/en-us/aspnet/core/signalr/hub-filters?view=aspnetcore-10.0
/// 
/// </remarks>
public sealed class MonitorHubFilter : IHubFilter
{
    private readonly IServerMonitor _monitor;

    public MonitorHubFilter(IServerMonitor monitor)
    {
        _monitor = monitor;
    }

    /// <summary>
    /// Adds a connection only after the connection pipeline succeeds (prevents "false active connections")
    /// </summary>
    /// <param name="context">Context about the connecting client.</param>
    /// <param name="next">Delegate that invokes the next operation in the connection pipeline.</param>
    /// <returns>A task representing the asynchronous connection operation</returns>
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        await next(context);
        // if failure then no connection
        _monitor.Connected(context.Context.ConnectionId);
    }

    /// <summary>
    /// Removes a connection even if the disconnection pipeline fails (prevents "stale entries")
    /// </summary>
    /// <param name="context">Context about the disconnecting client.</param>
    /// <param name="exception">Exception associated with the disconnection</param>
    /// <param name="next">Delegate that invokes the next operation in the disconnection pipeline.</param>
    /// <returns>A task representing the asynchronous disconnection operation.</returns>
    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        try
        {
            await next(context, exception);
        }
        finally
        {
            // no matter success or failure disconnect
            _monitor.Disconnected(context.Context.ConnectionId);
        }
    }
}
