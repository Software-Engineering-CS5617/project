using System.Collections.ObjectModel;
using Cloud.Networking;
using Cloud.UI.Commands;
using Cloud.UI.Models;
using Cloud.UI.Services;
using Microsoft.UI.Dispatching;

namespace Cloud.UI.ViewModels;

/// <summary>
/// View model of the Notification Service page
/// </summary>
public sealed class NotificationServiceViewModel : ObservableObject, IAsyncDisposable
{
    private readonly NotificationService _service;
    private readonly Action<Action> _runOnUi;
    private readonly HashSet<Guid> _seen = new();

    private ConnectionState _state = ConnectionState.Disconnected;
    private bool _isBusy;
    private string _serverUrl;
    private string _messageText = string.Empty;
    private string? _errorText;
    private string _lastSyncText = "Not synced yet";
    private DateTimeOffset _lastMessageTime = DateTimeOffset.MinValue;

    /// <param name="createMessenger">Creates a messenger for a server URL, e.g. <c>url =&gt; MessengerFactory.GetMessenger(url)</c>.</param>
    /// <param name="runOnUi">Runs an action on the UI thread. Defaults to the current thread's DispatcherQueue; tests can pass <c>action =&gt; action()</c>.</param>
    public NotificationServiceViewModel(Func<string, IMessenger> createMessenger, Action<Action>? runOnUi = null)
    {
        ArgumentNullException.ThrowIfNull(createMessenger);
        if (runOnUi is null)
        {
            DispatcherQueue queue = DispatcherQueue.GetForCurrentThread()
                                    ?? throw new InvalidOperationException("Create the view model on the UI thread.");
            runOnUi = action => queue.TryEnqueue(() => action());
        }

        _runOnUi = runOnUi;

        _service = new NotificationService(createMessenger);
        _service.ServerChanged += OnServerChanged;
        _service.MessageReceived += OnMessageReceived;
        _service.Reconnecting += OnReconnecting;
        _service.Reconnected += OnReconnected;
        _service.Closed += OnClosed;

        _serverUrl = string.Empty;

        // Connect and Reconnect do the same thing (connect, starting from a clean connection);
        // they only differ in when they are available.
        ConnectCommand = new ConnectCommand(this, _service);
        ReconnectCommand = new ReconnectCommand(this, _service);
        DisconnectCommand = new DisconnectCommand(this, _service);
        SendCommand = new SendCommand(this, _service);
        SyncCommand = new SyncCommand(this, _service);
        ClearCommand = new ClearCommand(this);

        Notifications.CollectionChanged += (_, _) => {
            OnPropertyChanged(nameof(IsEmpty));
            ClearCommand.RaiseCanExecuteChanged();
        };
    }


    /// <summary>Raised on the UI thread for each new live notification (not for synced backlog items).</summary>
    public event Action<NotificationItem>? NotificationReceived;

    /// <summary>Notifications, newest first.</summary>
    public ObservableCollection<NotificationItem> Notifications { get; } = new();

    /// <summary>Text of the server URL box.</summary>
    public string ServerUrl
    {
        get => _serverUrl;
        set {
            if (SetProperty(ref _serverUrl, value))
            {
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ConnectionState State
    {
        get => _state;
        internal set {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(StateText));
                OnPropertyChanged(nameof(StateHint));
                RaiseCommandStates();
            }
        }
    }

    public string StateText => State switch {
        ConnectionState.Connected => "Connected",
        ConnectionState.Connecting => "Connecting…",
        ConnectionState.Reconnecting => "Reconnecting…",
        _ => "Disconnected"
    };

    public string StateHint => State switch {
        ConnectionState.Connected => "Listening for notifications.",
        ConnectionState.Connecting => "Connecting to the server…",
        ConnectionState.Reconnecting => "Connection lost. Trying to reconnect…",
        _ => "Not connected. You won't receive notifications."
    };

    public string MessageText
    {
        get => _messageText;
        set {
            if (SetProperty(ref _messageText, value))
            {
                SendCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ErrorText
    {
        get => _errorText;
        internal set {
            if (SetProperty(ref _errorText, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    public string LastSyncText
    {
        get => _lastSyncText;
        internal set => SetProperty(ref _lastSyncText, value);
    }

    public bool IsEmpty => Notifications.Count == 0;

    public ConnectCommand ConnectCommand { get; }
    public ReconnectCommand ReconnectCommand { get; }
    public DisconnectCommand DisconnectCommand { get; }
    public SendCommand SendCommand { get; }
    public SyncCommand SyncCommand { get; }
    public ClearCommand ClearCommand { get; }


    /// <summary>True while a command is running; all commands are unavailable then.</summary>
    internal bool IsBusy
    {
        get => _isBusy;
        set {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    internal bool IsOnline => State == ConnectionState.Connected || State == ConnectionState.Reconnecting;

    /// <summary>Timestamp of the newest message shown; the starting point of a sync.</summary>
    internal DateTimeOffset LastMessageTime => _lastMessageTime;

    /// <summary>Adds a message to the list. Returns the new item, or null when it was already shown.</summary>
    internal NotificationItem? AddNotification(Message message)
    {
        if (!_seen.Add(message.Id))
        {
            return null;
        }

        NotificationItem item = new() {
            Id = message.Id,
            SenderId = message.SenderId,
            Content = message.Content,
            ReceivedAt = message.Timestamp
        };
        Notifications.Insert(0, item);

        if (message.Timestamp > _lastMessageTime)
        {
            _lastMessageTime = message.Timestamp;
        }

        return item;
    }


    private void RaiseCommandStates()
    {
        ConnectCommand.RaiseCanExecuteChanged();
        ReconnectCommand.RaiseCanExecuteChanged();
        DisconnectCommand.RaiseCanExecuteChanged();
        SendCommand.RaiseCanExecuteChanged();
        SyncCommand.RaiseCanExecuteChanged();
    }

    /// <summary>The old server's notifications must not be mixed with the new server's.</summary>
    private void OnServerChanged()
    {
        Notifications.Clear();
        _seen.Clear();
        _lastMessageTime = DateTimeOffset.MinValue;
        LastSyncText = "Not synced yet";
    }

    // Messenger events (background threads -> UI thread)

    private void OnMessageReceived(Message message)
    {
        _runOnUi(() => {
            if (AddNotification(message) is { } item)
            {
                NotificationReceived?.Invoke(item);
            }
        });
    }

    private void OnReconnecting(Exception? _)
    {
        _runOnUi(() => {
            State = _service.State;
        });
    }

    private void OnReconnected(string? _)
    {
        _runOnUi(() => {
            ErrorText = null;
            State = _service.State;
        });
    }

    private void OnClosed(Exception? exception)
    {
        _runOnUi(() => {
            if (exception is not null)
            {
                ErrorText = $"Connection closed: {exception.Message}";
            }

            State = _service.State;
        });
    }

    public ValueTask DisposeAsync()
    {
        return _service.DisposeAsync();
    }
}