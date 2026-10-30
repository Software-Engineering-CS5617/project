// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Networking;

/// <summary>
/// Implements messaging using SignalR.
/// </summary>
public sealed class SignalRMessenger : IMessenger, IAsyncDisposable
{
    private readonly HubConnection _connection;
    public event Action<Message>? MessageReceived;

    /// <summary>
    /// Build a hub connection and register a handler.
    /// </summary>
    /// <param name="serverurl"></param>
    public SignalRMessenger(string serverUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<Message>(
            Contract.ReceiveMessage,
            message => MessageReceived?.Invoke(message));
    }

    /// <summary>
    /// Connects to the server.
    /// </summary>
    public async Task ConnectAsync()
    {
        await _connection.StartAsync();
    }

    /// <summary>
    /// Sends the content of a message to the server.
    /// </summary>
    public async Task SendMessageAsync(string content)
    {
        await _connection.InvokeAsync(Contract.SendMessage, content);
    }

    /// <summary>
    /// Disconnects from the server (object can be reused).
    /// </summary>
    public async Task DisconnectAsync()
    {
        await _connection.StopAsync();
    }

    /// <summary>
    /// Disposes off the connection (object cannot be reused).
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
