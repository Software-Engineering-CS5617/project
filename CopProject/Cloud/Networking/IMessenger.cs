// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Networking;

/// <summary>
/// Clients use this interface which helps them communicate with the server.
/// </summary>
public interface IMessenger : IAsyncDisposable
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    HubConnectionState State { get; }

    /// <summary>
    /// Asynchronously connects to the server.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task ConnectAsync();

    /// <summary>
    /// Asynchronously sends content of message to the server.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task SendMessageAsync(string content);

    /// <summary>
    /// Asynchronously disconnects from the server (but does not dispose the connection)
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Notify the subscribers when a message is received.
    /// </summary>
    event Action<Message>? MessageReceived;

    /// <summary>
    /// Notify the subscribers when the connection is lost and the client is trying to reconnect.
    /// </summary>
    event Action<Exception?>? Reconnecting;

    /// <summary>
    /// Notify the subscribers when the client reconnects to the server (with a new connection ID).
    /// </summary>
    event Action<string?>? Reconnected;

    /// <summary>
    /// Notify the subscribers when the connection is closed (either normally or due to an error)
    /// </summary>
    event Action<Exception?>? Closed;
}
