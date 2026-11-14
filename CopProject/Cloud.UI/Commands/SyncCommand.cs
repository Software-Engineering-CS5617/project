using Cloud.Networking;
using Cloud.UI.Services;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>Fetches the messages missed since the newest one already shown.</summary>
public sealed class SyncCommand : NotificationCommand
{
    public SyncCommand(NotificationServiceViewModel viewModel, NotificationService service)
        : base(viewModel, service)
    {
    }

    protected override bool IsAvailable()
    {
        return ViewModel.State == ConnectionState.Connected;
    }

    protected override async Task RunAsync()
    {
        IReadOnlyList<Message> missed = await Service.SyncAsync(ViewModel.LastMessageTime);

        // Oldest first, so the newest ends up on top. No alerts for backlog items.
        int added = 0;
        foreach (Message message in missed.OrderBy(m => m.Timestamp))
        {
            if (ViewModel.AddNotification(message) is not null)
            {
                added++;
            }
        }

        ViewModel.LastSyncText = $"Last synced {DateTime.Now:t} · {added} new";
    }
}
