// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Networking.Mocking;

/// <summary>
/// Pass-Through Proxy wraps around <see cref ="HubConnection" /> object and
/// inherits from <see cref ="IHubConnection" /> interface which exposes a
/// subset of <see cref ="HubConnection" /> features [all of this to mock <see cref ="HubConnection" />]
/// </summary>
///
/// <remarks>
/// Reference:
/// <see href="https://stackoverflow.com/questions/65009412/how-to-mock-a-wrapped-microsoft-asp-net-core-client-signalr-hubconnection-class">
/// Mocking a wrapped Microsoft ASP.NET Core SignalR HubConnection
/// </see>
/// </remarks>
internal sealed class HubConnectionProxy : IHubConnection
{
    private readonly HubConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="HubConnectionProxy"/> class (Pass-Through Proxy)
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null</exception>
    public HubConnectionProxy(HubConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _connection = connection;
    }

    /// <summary>
    /// Pass-through proxy (for mocking) for <see cref="HubConnectionState"/>.
    /// </summary>
    public HubConnectionState State => _connection.State;

    /// <summary>
    /// Pass-Through Proxy (for mocking) for <see cref="HubConnection.Reconnecting"/>.
    /// </summary>
    public event Func<Exception?, Task>? Reconnecting
    {
        add => _connection.Reconnecting += value;
        remove => _connection.Reconnecting -= value;
    }

    /// <summary>
    /// Pass-Through Proxy (for mocking) for <see cref="HubConnection.Reconnected"/>.
    /// </summary>
    public event Func<string?, Task>? Reconnected
    {
        add => _connection.Reconnected += value;
        remove => _connection.Reconnected -= value;
    }

    /// <summary>
    /// Pass-Through Proxy (for mocking) for <see cref="HubConnection.Closed"/>.
    /// </summary>
    public event Func<Exception?, Task>? Closed
    {
        add => _connection.Closed += value;
        remove => _connection.Closed -= value;
    }

    /// <summary>
    /// Pass-Through Proxy (for mocking) for <see cref="HubConnectionExtensions.On{T}"/>.
    /// </summary>
    public IDisposable On<T>(string methodName, Action<T> handler)
    {
        return _connection.On(methodName, handler);
    }

    /// <summary>
    /// Pass-Through Proxy (for mocking) for <see cref="HubConnection.StartAsync"/>.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return _connection.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Pass-Through Proxy (for mocking) for <see cref="HubConnection.StopAsync"/>.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _connection.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Pass-Through Proxy (for mocking) for <see cref="HubConnectionExtensions.InvokeAsync"/>.
    /// </summary>
    public Task InvokeAsync(string methodName, object? arg, CancellationToken cancellationToken = default)
    {
        return _connection.InvokeAsync(methodName, arg, cancellationToken);
    }

    /// <summary>
    /// Pass-Through Proxy (for mocking) for the generic <c>HubConnectionExtensions.InvokeAsync&lt;TResult&gt;</c>.
    /// </summary>
    public Task<T> InvokeAsync<T>(string methodName, object? arg, CancellationToken cancellationToken = default)
    {
        return _connection.InvokeAsync<T>(methodName, arg, cancellationToken);
    }

    /// <summary>
    /// Pass-Through Proxy (for mocking) for <see cref="HubConnection.DisposeAsync"/>.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}
