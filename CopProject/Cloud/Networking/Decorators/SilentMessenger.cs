// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Decorators;

/// <summary>
/// Decorator for <see cref="IMessenger"/> that swallows exceptions thrown by the
/// inner messenger, so callers never have to handle network failures.
/// Callers can inspect <see cref="State"/> or the <see cref="Closed"/> event
/// to find out that something went wrong.
/// </summary>
internal sealed class SilentMessenger : IMessenger
{
    private readonly IMessenger _messenger;

    public ConnectionState State => _messenger.State;

    public event Action<Message>? MessageReceived;
    public event Action<Exception?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    /// <summary>
    /// Creates an instance of <cref name="SilentMessenger"/> class.
    /// </summary>
    /// <param name="messenger"></param>
    public SilentMessenger(IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(messenger);

        _messenger = messenger;

        _messenger.MessageReceived += OnMessageReceived;
        _messenger.Reconnecting += OnReconnecting;
        _messenger.Reconnected += OnReconnected;
        _messenger.Closed += OnClosed;
    }

    /// <summary>
    /// Like <see cref="IMessenger.ConnectAsync"/> but swallows errors
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            await _messenger.ConnectAsync();
        }
        catch
        {
            ;
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.SendMessageAsync"/> but swallows errors
    /// </summary>
    public async Task SendMessageAsync(string content)
    {
        try
        {
            await _messenger.SendMessageAsync(content);
        }
        catch
        {
            ;
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.SyncAsync"/> but swallows errors
    /// </summary>
    public async Task<IReadOnlyList<Message>> SyncAsync(DateTimeOffset since)
    {
        try
        {
            return await _messenger.SyncAsync(since);
        }
        catch
        {
            return Array.Empty<Message>();
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.DisconnectAsync"/> but swallows errors
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            await _messenger.DisconnectAsync();
        }
        catch
        {
            ;
        }
    }

    private void OnMessageReceived(Message message)
    {
        MessageReceived?.Invoke(message);
    }

    private void OnReconnecting(Exception? exception)
    {
        Reconnecting?.Invoke(exception);
    }

    private void OnReconnected(string? connectionId)
    {
        Reconnected?.Invoke(connectionId);
    }

    private void OnClosed(Exception? exception)
    {
        Closed?.Invoke(exception);
    }

    /// <summary>
    /// Like <see cref="IAsyncDisposable.DisposeAsync"/> but swallows errors
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _messenger.MessageReceived -= OnMessageReceived;
        _messenger.Reconnecting -= OnReconnecting;
        _messenger.Reconnected -= OnReconnected;
        _messenger.Closed -= OnClosed;

        try
        {
            await _messenger.DisposeAsync();
        }
        catch
        {
            ;
        }
    }
}
