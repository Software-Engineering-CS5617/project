// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Networking;

/// <summary>
/// Interface reflecting a subset of features in <see cref="HubConnection"/> class (for mocking it).
/// </summary>
/// 
/// <remarks>
/// <list type="bullet">
/// <item>
/// <description><see cref="HubConnection"/> does not expose an interface for mocking.</description>
/// </item>
/// <item>
/// <description>SignalR extension methods such as <c>On</c> and <c>InvokeAsync</c> cannot be mocked directly.</description>
/// </item>
/// </list>
///
/// Reference:
/// <see href="https://stackoverflow.com/questions/65009412/how-to-mock-a-wrapped-microsoft-asp-net-core-client-signalr-hubconnection-class">
/// Mocking a wrapped Microsoft ASP.NET Core SignalR HubConnection
/// </see>
/// </remarks>
public interface IHubConnection : IAsyncDisposable
{
    /// <summary>
    /// Uses the same connection state as in <see cref="HubConnectionState"/>.
    /// </summary>
    HubConnectionState State { get; }

    /// <summary>
    /// Uses the same delegate signature as <see cref="HubConnection.Reconnecting"/>.
    /// </summary>
    event Func<Exception?, Task>? Reconnecting;

    /// <summary>
    /// Uses the same delegate signature as <see cref="HubConnection.Reconnected"/>.
    /// </summary>
    event Func<string?, Task>? Reconnected;

    /// <summary>
    /// Uses the same delegate signature as <see cref="HubConnection.Closed"/>.
    /// </summary>
    event Func<Exception?, Task>? Closed;

    /// <summary>
    /// Uses the same signature as <see cref="HubConnectionExtensions.On{T}"/>.
    /// </summary>
    IDisposable On<T>(string methodName, Action<T> handler);

    /// <summary>
    /// Uses the same signature as <see cref="HubConnection.StartAsync"/>.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uses the same signature as <see cref="HubConnection.StopAsync"/>.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uses the same signature as <see cref="HubConnectionExtensions.InvokeAsync"/>.
    /// </summary>
    Task InvokeAsync(string methodName, object? arg, CancellationToken cancellationToken = default);
}
