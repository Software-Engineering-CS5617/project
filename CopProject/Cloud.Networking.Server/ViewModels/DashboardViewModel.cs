// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace Cloud.Networking.Server.ViewModels;

/// <summary>
/// The view model for Dashboard
/// </summary>
public sealed class DashboardViewModel : IDisposable
{
    /// <summary>How many of the newest messages the dashboard shows.</summary>
    public const int RecentMessageCount = 10_000;

    /// <summary>The longest message the dashboard (and the hub) accepts.</summary>
    public const int MaxLength = MessageHub.MaxContentLength;

    private readonly IServerMonitor _monitor;
    private readonly IMessageStore _store;
    private readonly IHubContext<MessageHub> _hub; /* to call hub methods outside of hub we use IHubContext */

    /// <summary>Lets only one refresh run at a time, so an older result can never overwrite a newer one.</summary>
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private volatile bool _disposed;

    /// <summary>Initializes the dashboard view model.</summary>
    /// <param name="monitor">The monitor providing connection and message info</param>
    /// <param name="store">Where server messages are kept for later sync</param>
    /// <param name="hub">The hub context used to reach connected clients</param>
    public DashboardViewModel(IServerMonitor monitor, IMessageStore store, IHubContext<MessageHub> hub)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(hub);

        _monitor = monitor;
        _store = store;
        _hub = hub;
        _monitor.Changed += OnMonitorChanged;
    }

    public event Action? Changed;

    public string Text { get; set; } = "";

    public string? Status { get; private set; }

    public bool StatusIsError { get; private set; }

    public bool IsSending { get; private set; }

    /// <summary>Connections formatted for display (rebuilt by <see cref="RefreshAsync"/>).</summary>
    public IReadOnlyList<ConnectionItem> Connections { get; private set; } = [];

    /// <summary>Newest messages formatted for display (rebuilt by <see cref="RefreshAsync"/>).</summary>
    public IReadOnlyList<MessageItem> Messages { get; private set; } = [];

    /// <summary>
    /// Whether a message can be sent. Connected clients are not required:
    /// the message is stored, so offline clients get it when they sync.
    /// </summary>
    public bool CanSend => !IsSending && !string.IsNullOrWhiteSpace(Text) && Text.Trim().Length <= MaxLength;

    /// <summary>Rebuilds the display data, then tells the view. Called on page start and on every monitor change.</summary>
    /// <returns>A task representing the asynchronous refresh.</returns>
    public async Task RefreshAsync()
    {
        if (_disposed)
        {
            return;
        }

        // one refresh at a time
        await _refreshGate.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            // snapshot connections now, so both lists describe (almost) the same moment
            IReadOnlyList<ConnectionItem> connections =
                [.. _monitor.Connections.Select(c => new ConnectionItem(c.ConnectionId, FormatTime(c.ConnectedAt)))];

            IReadOnlyList<MessageItem>? messages = null;

            try
            {
                IReadOnlyList<Message> recent = await _monitor.GetRecentMessagesAsync(RecentMessageCount);

                messages = [.. recent.Select(m => new MessageItem(
                    m.Id.ToString("N")[..8],
                    FormatTime(m.Timestamp),
                    m.SenderId,
                    m.Content,
                    m.SenderId == MessageHub.ServerSenderId))];
            }
            catch (Exception ex)
            {
                Status = $"Could not load messages: {ex.Message}";
                StatusIsError = true;
            }

            // swap both lists in one step (no await in between) so the view never renders a half-updated state
            Connections = connections;
            if (messages is not null)
            {
                Messages = messages;
            }
        }
        finally
        {
            _refreshGate.Release();
        }

        Changed?.Invoke();
    }

    /// <summary>Stores the current text, sends it to all clients and updates the status.</summary>
    /// <returns>A task representing the asynchronous send operation.</returns>
    public async Task SendAsync()
    {
        if (!CanSend)
        {
            return;
        }

        IsSending = true;
        Changed?.Invoke();

        try
        {
            Message message = new() {
                SenderId = MessageHub.ServerSenderId,
                Content = Text.Trim()
            };

            // Store first: if the push below fails, clients can still get it by syncing.
            // The store raises MessageAdded, which is how the monitor/dashboard find out.
            await _store.AddAsync(message);
            await _hub.Clients.All.SendAsync(Contract.ReceiveMessage, message);

            int online = _monitor.ConnectionCount;
            Status = online == 0
                ? "Saved. No clients are online; they will get it when they sync."
                : $"Sent to {online} connected client(s). Offline clients get it when they sync.";
            StatusIsError = false;
            Text = "";
        }
        catch (Exception ex)
        {
            Status = $"Failed to send: {ex.Message}";
            StatusIsError = true;
        }
        finally
        {
            IsSending = false;
            Changed?.Invoke();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _monitor.Changed -= OnMonitorChanged;
    }

    private static string FormatTime(DateTimeOffset time)
    {
        return time.ToLocalTime().ToString("HH:mm:ss");
    }

    private void OnMonitorChanged()
    {
        _ = RefreshAsync();
    }
}
