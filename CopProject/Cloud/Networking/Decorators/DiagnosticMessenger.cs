// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Mocking;

namespace Cloud.Networking.Decorators;

/// <summary>
/// Decorator for <see cref="IMessenger"/> that adds diagnostic tracing.
/// any exceptionis traced and then rethrown, so callers still see failures.
/// </summary>
internal sealed class DiagnosticMessenger : IMessenger
{
    private readonly IMessenger _messenger;
    private readonly ITracer _tracer;

    public ConnectionState State => _messenger.State;

    public event Action<Message>? MessageReceived;
    public event Action<Exception?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    /// <summary>
    /// Creates an instance of <see cref="DiagnosticMessenger"/> class.
    /// </summary>
    /// <param name="messenger">The messenger to decorate</param>
    /// <param name="tracer">Where the trace lines are written</param>
    public DiagnosticMessenger(IMessenger messenger, ITracer tracer)
    {
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(tracer);

        _messenger = messenger;
        _tracer = tracer;

        _messenger.MessageReceived += OnMessageReceived;
        _messenger.Reconnecting += OnReconnecting;
        _messenger.Reconnected += OnReconnected;
        _messenger.Closed += OnClosed;
    }

    /// <summary>
    /// Like <see cref="IMessenger.ConnectAsync"/> but traces the attempt and its outcome
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            _tracer.WriteLine("-- connecting");
            await _messenger.ConnectAsync();
            _tracer.WriteLine("-- connected");
        }
        catch (Exception ex)
        {
            _tracer.WriteLine($"-- connection failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.SendMessageAsync"/> but traces the outcome
    /// </summary>
    public async Task SendMessageAsync(string content)
    {
        try
        {
            await _messenger.SendMessageAsync(content);
            _tracer.WriteLine("-- message sent");
        }
        catch (Exception ex)
        {
            _tracer.WriteLine($"-- message sending failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.SyncAsync"/> but traces how many messages were synced
    /// </summary>
    public async Task<IReadOnlyList<Message>> SyncAsync(DateTimeOffset since)
    {
        try
        {
            IReadOnlyList<Message> missed = await _messenger.SyncAsync(since);
            _tracer.WriteLine($"-- synced {missed.Count} message(s)");
            return missed;
        }
        catch (Exception ex)
        {
            _tracer.WriteLine($"-- sync failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Like <see cref="IMessenger.DisconnectAsync"/> but traces the outcome
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            await _messenger.DisconnectAsync();
            _tracer.WriteLine("-- disconnected");
        }
        catch (Exception ex)
        {
            _tracer.WriteLine($"-- disconnection failed: {ex}");
            throw;
        }
    }

    private void OnMessageReceived(Message message)
    {
        _tracer.WriteLine($"-- message received from {message.SenderId}");
        MessageReceived?.Invoke(message);
    }

    private void OnReconnecting(Exception? exception)
    {
        _tracer.WriteLine($"-- reconnecting: {exception?.Message}");
        Reconnecting?.Invoke(exception);
    }

    private void OnReconnected(string? connectionId)
    {
        _tracer.WriteLine($"-- reconnected: {connectionId}");
        Reconnected?.Invoke(connectionId);
    }

    private void OnClosed(Exception? exception)
    {
        _tracer.WriteLine($"-- connection closed: {exception?.Message}");
        Closed?.Invoke(exception);
    }

    /// <summary>
    /// Like <see cref="IAsyncDisposable.DisposeAsync"/> but traces a failure
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
        catch (Exception ex)
        {
            _tracer.WriteLine($"-- disposal failed: {ex}");
            throw;
        }
    }
}
