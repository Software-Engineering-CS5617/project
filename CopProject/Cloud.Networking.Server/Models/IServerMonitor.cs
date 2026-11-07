// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Abstraction over the server monitor so that consumers can be tested with mocks.
/// </summary>
public interface IServerMonitor
{
    /// <summary>Raised whenever the monitored connections or messages change.</summary>
    event Action? Changed;

    /// <summary>Gets the currently active server connections.</summary>
    IReadOnlyList<LiveConnection> Connections { get; }

    /// <summary>Gets the messages recorded by the server.</summary>
    IReadOnlyList<Message> Messages { get; }

    /// <summary>Creates a new client connection.</summary>
    /// <param name="connectionId">The unique identifier of the connection.</param>
    void Connected(string connectionId);

    /// <summary>Removes a client connection from the monitor.</summary>
    /// <param name="connectionId">The connection id to remove.</param>
    void Disconnected(string connectionId);

    /// <summary>Records a message received by the server.</summary>
    /// <param name="message">The message to record.</param>
    void RecordMessage(Message message);
}
