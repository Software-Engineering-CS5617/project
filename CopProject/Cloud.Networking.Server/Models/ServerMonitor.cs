// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Monitors live server connections and message changed signals.
/// </summary>
public sealed class ServerMonitor : IServerMonitor
{
    private readonly object _gate = new();
    private readonly Dictionary<string, DateTimeOffset> _connections = [];
    private readonly IMessageStore _store;

    /// <summary>
    /// invoked on addition of messages or addition or removal of connections
    /// </summary>
    public event Action? Changed;

    /// <summary>Any message added to the store notifies listeners.</summary>
    private void OnMessageAdded(Message _)
    {
        Changed?.Invoke();
    }

    /// <summary>Initializes the monitor and starts listening to the store for new messages.</summary>
    /// <param name="store">The store that owns the messages.</param>
    public ServerMonitor(IMessageStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _store.MessageAdded += OnMessageAdded;
    }


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
    public int ConnectionCount
    {
        get {
            lock (_gate)
            {
                return _connections.Count;
            }
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Message>> GetRecentMessagesAsync(int count)
    {
        return _store.GetRecentAsync(count);
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

    /// <summary>Stops listening to the store.</summary>
    public void Dispose()
    {
        _store.MessageAdded -= OnMessageAdded;
    }
}
