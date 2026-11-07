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
    private readonly IServerMonitor _monitor;
    private readonly IHubContext<MessageHub> _hub; /* to call hub methods outside of hub we use IHubContext*/
    private bool _sending;

    /// <summary>Initializes the dashboard view model with the server monitor and hub context.</summary>
    /// <param name="monitor">The monitor providing connection and message info</param>
    /// <param name="hub">The hub context used to send messages to connected clients (outside of hub)</param>
    public DashboardViewModel(IServerMonitor monitor, IHubContext<MessageHub> hub)
    {
        _monitor = monitor;
        _hub = hub;
        _monitor.Changed += OnMonitorChanged; /* to propagate changes from monitor to VM.Changed*/
    }

    /// <summary>Raised whenever the dashboard view model state changes.</summary>
    public event Action? Changed;

    /// <summary>Gets or sets the message text to be sent to connected clients.</summary>
    public string Text { get; set; } = "";

    /// <summary>Gets the current message sending status.</summary>
    public string? Status { get; private set; }

    /// <summary>Gets whether the current status represents an error.</summary>
    public bool StatusIsError { get; private set; }

    /// <summary>Gets the server connections formatted for dashboard display.</summary>
    public IReadOnlyList<ConnectionItem> Connections => _monitor.Connections
        .Select(c => new ConnectionItem(c.ConnectionId, c.ConnectedAt.ToLocalTime().ToString("HH:mm:ss")))
        .ToList();

    /// <summary>Gets the recorded messages formatted for dashboard display.</summary>
    public IReadOnlyList<MessageItem> Messages => _monitor.Messages
        .Select(m => new MessageItem(
            m.Id.ToString("N")[..8],
            m.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
            m.SenderId,
            m.Content,
            m.SenderId == ServerMonitor.ServerSenderId))
        .ToList();

    /// <summary>Gets whether a message can currently be sent.</summary>
    public bool CanSend => !_sending && !string.IsNullOrWhiteSpace(Text) && _monitor.Connections.Count > 0;

    /// <summary>Sends the current message to all connected clients and updates the sneding status, error, text ...</summary>
    /// <returns>A task representing the asynchronous send operation.</returns>
    public async Task SendAsync()
    {
        if (!CanSend)
        {
            return;
        }

        _sending = true;
        Changed?.Invoke(); // to tell view to disable the button

        try
        {
            Message message = new() {
                SenderId = ServerMonitor.ServerSenderId,
                Content = Text.Trim()
            };

            await _hub.Clients.All.SendAsync(Contract.ReceiveMessage, message);
            _monitor.RecordMessage(message);

            Status = $"Sent to {_monitor.Connections.Count} connected client(s).";
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
            _sending = false;
            Changed?.Invoke(); // re-enable the button
        }
    }

    /// <summary>On server monitor changes, it will propagate here</summary>
    private void OnMonitorChanged()
    {
        Changed?.Invoke();
    }

    /// <summary>Unsubscribes from server monitor</summary>
    public void Dispose()
    {
        _monitor.Changed -= OnMonitorChanged;
    }
}
