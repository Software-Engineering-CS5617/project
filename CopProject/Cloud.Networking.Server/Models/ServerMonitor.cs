// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Maintains an in-memory snapshot of server connections and messages.
/// </summary>
public sealed class ServerMonitor : IServerMonitor
{
    /// <summary>Identifier used for messages originating from the server.</summary>
    public const string ServerSenderId = "server";
    private const int MaxMessages = 200;

    private readonly object _gate = new();
    private readonly Dictionary<string, DateTimeOffset> _connections = [];
    private readonly Queue<Message> _messages = new();

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public IReadOnlyList<LiveConnection> Connections
    {
        get {
            lock (_gate)
            {
                return _connections
                    .Select(x => new LiveConnection(x.Key, x.Value))
                    .OrderBy(x => x.ConnectedAt)
                    .ToList();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Message> Messages
    {
        get {
            lock (_gate)
            {
                // Newest first
                return _messages.Reverse().ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void Connected(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            _connections[connectionId] = DateTimeOffset.UtcNow;
        }

        Changed?.Invoke();
    }

    /// <inheritdoc />
    public void Disconnected(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            _connections.Remove(connectionId);
        }

        Changed?.Invoke();
    }

    /// <inheritdoc />
    public void RecordMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_gate)
        {
            _messages.Enqueue(message);

            if (_messages.Count > MaxMessages)
            {
                _messages.Dequeue();
            }
        }

        Changed?.Invoke();
    }
}
