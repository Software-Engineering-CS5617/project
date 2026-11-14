using Cloud.Networking;

namespace Cloud.UI.Services;

/// <summary>
/// Owns the messenger (the connection to the server) 
/// </summary>
public sealed class NotificationService : IAsyncDisposable
{
    private readonly Func<string, IMessenger> _createMessenger;

    private IMessenger? _messenger;
    private string _messengerUrl = string.Empty;

    /// <param name="createMessenger">Creates a messenger for a server URL, e.g. <c>url =&gt; MessengerFactory.GetMessenger(url)</c>.</param>
    public NotificationService(Func<string, IMessenger> createMessenger)
    {
        ArgumentNullException.ThrowIfNull(createMessenger);
        _createMessenger = createMessenger;
    }

    /// <summary>Raised on the calling thread when the old messenger is dropped for a different server.</summary>
    public event Action? ServerChanged;

    public event Action<Message>? MessageReceived;
    public event Action<Exception?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    /// <summary>State of the current messenger; Disconnected when there is none.</summary>
    public ConnectionState State => _messenger?.State ?? ConnectionState.Disconnected;

    private IMessenger Messenger => _messenger ?? throw new InvalidOperationException("Not connected.");

    /// <summary>Accepts only absolute http/https URLs; <paramref name="url"/> is then the normalised form.</summary>
    public static bool TryParseUrl(string text, out string url)
    {
        if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            url = uri.AbsoluteUri;
            return true;
        }

        url = string.Empty;
        return false;
    }

    /// <summary>
    /// Makes the service ready to connect to <paramref name="url"/> from a clean state:
    /// same server: the messenger is reused; different server: it is replaced.
    /// An existing connection is closed.
    /// </summary>
    public async Task UseServerAsync(string url)
    {
        // Same server: reuse the messenger. Different server: replace it.
        IMessenger messenger = _messenger is not null && url == _messengerUrl
            ? _messenger
            : await SwitchServerAsync(url);

        if (messenger.State != ConnectionState.Disconnected)
        {
            await messenger.DisconnectAsync();
        }
    }

    /// <summary>Connects to the server chosen with <see cref="UseServerAsync"/>.</summary>
    public async Task ConnectAsync()
    {
        await Messenger.ConnectAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_messenger is not null)
        {
            await _messenger.DisconnectAsync();
        }
    }

    public async Task SendAsync(string text)
    {
        await Messenger.SendMessageAsync(text);
    }

    /// <summary>Messages newer than <paramref name="since"/> that the server has.</summary>
    public async Task<IReadOnlyList<Message>> SyncAsync(DateTimeOffset since)
    {
        IReadOnlyList<Message> missed = await Messenger.SyncAsync(since);
        return missed;
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(DisposeMessengerAsync());
    }

    /// <summary>Drops the current messenger (and the old server's notifications) and creates one for <paramref name="url"/>.</summary>
    private async Task<IMessenger> SwitchServerAsync(string url)
    {
        await DisposeMessengerAsync();
        ServerChanged?.Invoke();

        IMessenger messenger = _createMessenger(url);
        messenger.MessageReceived += OnMessageReceived;
        messenger.Reconnecting += OnReconnecting;
        messenger.Reconnected += OnReconnected;
        messenger.Closed += OnClosed;

        _messenger = messenger;
        _messengerUrl = url;
        return messenger;
    }

    private async Task DisposeMessengerAsync()
    {
        IMessenger? messenger = _messenger;
        if (messenger is null)
        {
            return;
        }

        _messenger = null;
        messenger.MessageReceived -= OnMessageReceived;
        messenger.Reconnecting -= OnReconnecting;
        messenger.Reconnected -= OnReconnected;
        messenger.Closed -= OnClosed;

        try
        {
            await messenger.DisposeAsync();
        }
        catch
        {
            // Best effort: the connection is being discarded anyway.
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
}
