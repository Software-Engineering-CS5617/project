// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking;

/// <summary>
/// Clients use this interface which helps them communicate with the server.
/// </summary>
/// <remarks>
/// Events are raised on a background (thread-pool) thread.
/// UI consumers must marshal to their own UI thread before touching UI state.
/// </remarks>
public interface IMessenger : IAsyncDisposable
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    ConnectionState State { get; }

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
    /// Asks the server for the messages created after <paramref name="since"/>
    /// (used to catch up on messages missed while disconnected).
    /// The result may include messages that were already received; callers should de-duplicate by <see cref="Message.Id"/>.
    /// </summary>
    /// <param name="since">Only messages with a later timestamp are returned. Use <see cref="DateTimeOffset.MinValue"/> for everything.</param>
    /// <returns>The missed messages (empty when there are none)</returns>
    Task<IReadOnlyList<Message>> SyncAsync(DateTimeOffset since);

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
