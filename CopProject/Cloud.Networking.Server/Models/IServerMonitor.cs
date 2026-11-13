// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Abstraction over the server monitor so that consumers can be tested with mocks.
/// </summary>
public interface IServerMonitor : IDisposable
{
    /// <summary>Raised whenever the monitored connections or messages change.</summary>
    event Action? Changed;

    /// <summary>Gets the currently active server connections, oldest first.</summary>
    IReadOnlyList<LiveConnection> Connections { get; }

    /// <summary>Gets the number of currently active connections (cheaper than <see cref="Connections"/>).</summary>
    int ConnectionCount { get; }

    /// <summary>Creates a new client connection.</summary>
    /// <param name="connectionId">The unique identifier of the connection.</param>
    void Connected(string connectionId);

    /// <summary>Removes a client connection from the monitor.</summary>
    /// <param name="connectionId">The connection id to remove.</param>
    void Disconnected(string connectionId);

    /// <summary>Gets the most recent messages (read from the message store), newest first.</summary>
    /// <param name="count">The maximum number of messages to return.</param>
    /// <returns>A read-only list of messages.</returns>
    Task<IReadOnlyList<Message>> GetRecentMessagesAsync(int count);
}
