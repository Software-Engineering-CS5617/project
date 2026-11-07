// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Networking;

/// <summary>
/// Implements IMessenger interface using SignalR.
/// It uses a connection which is <see cref="HubConnectionProxy"/> class that wraps 
/// <see cref="HubConnection"/> (all of this is just for mocking it)
/// </summary>
/// 
/// <remarks>
/// SignalR's events are <see cref="Func{T, Task}"/> delegates. 
/// This class makes them into <see cref="Action{T}"/> events,
/// so that consumers can subscribe with simple synchronous handlers, 
/// without having to return a <see cref="Task"/>.
/// </remarks>
public sealed class SignalRMessenger : IMessenger
{
    private readonly IHubConnection _connection;

    /// <inheritdoc />
    public HubConnectionState State => _connection.State;

    /// <inheritdoc/>
    public event Action<Exception?>? Reconnecting;

    /// <inheritdoc/>
    public event Action<string?>? Reconnected;

    /// <inheritdoc/>
    public event Action<Exception?>? Closed;

    /// <inheritdoc/>
    public event Action<Message>? MessageReceived;

    /// <summary>
    /// Private method that creates a <see cref="HubConnectionProxy"/> over a <see cref="HubConnection" /> object
    /// </summary>
    /// <param name="serverUrl">The url to the server</param>
    private static HubConnectionProxy CreateConnection(string serverUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);

        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        return new HubConnectionProxy(connection);
    }



    /// <summary>
    /// Public constructor: builds the real connection.
    /// </summary>
    /// <param name="serverUrl">The url to the server</param>
    public SignalRMessenger(string serverUrl)
        : this(CreateConnection(serverUrl))
    {
    }

    /// <summary>
    /// Internal constructor: lets you pass a mock IHubConnection.
    /// </summary>
    /// <param name="connection">The connection of type <see cref="IHubConnection"></see></param>
    internal SignalRMessenger(IHubConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _connection = connection;

        _connection.On<Message>(
            Contract.ReceiveMessage,
            message => MessageReceived?.Invoke(message));

        _connection.Reconnecting += exception => {
            Reconnecting?.Invoke(exception);
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId => {
            Reconnected?.Invoke(connectionId);
            return Task.CompletedTask;
        };

        _connection.Closed += exception => {
            Closed?.Invoke(exception);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Starts the connection to the SignalR server. <br />
    /// Makes up to three attempts, waiting 1 and then 2 seconds.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when state of the connection is Connecting or Reconnecting</exception>
    public async Task ConnectAsync()
    {
        /* if connected, then just return without throwing */
        if (_connection.State == HubConnectionState.Connected)
        {
            return;
        }

        /* i should create only for a disconnected connection */
        if (_connection.State != HubConnectionState.Disconnected)
        {
            throw new InvalidOperationException($"Cannot connect while the connection is in the {_connection.State} state.");
        }

        const int MaxAttempts = 3;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await _connection.StartAsync();
                return;
            }
            catch (Exception ex) when (
                attempt < MaxAttempts &&
                ex is not (ObjectDisposedException or InvalidOperationException
                    or ArgumentException or NotSupportedException or OperationCanceledException))
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }
    }

    /// <summary>
    /// Asynchronously sends content of message to the server.
    /// </summary>
    /// <param name="content">The message content to send</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is null or has only whitespace</exception>
    public async Task SendMessageAsync(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        await _connection.InvokeAsync(Contract.SendMessage, content);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        await _connection.StopAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
