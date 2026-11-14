using Cloud.Networking;
using Cloud.UI.Services;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>
/// Sends the typed message. 
/// The server broadcasts to everyone (including us), so the message
/// shows up in the list and as an alert.
/// </summary>
public sealed class SendCommand : NotificationCommand
{
    public SendCommand(NotificationServiceViewModel viewModel, NotificationService service)
        : base(viewModel, service)
    {
    }

    protected override bool IsAvailable()
    {
        return ViewModel.State == ConnectionState.Connected && ViewModel.MessageText.Trim().Length > 0;
    }

    protected override async Task RunAsync()
    {
        await Service.SendAsync(ViewModel.MessageText.Trim());
        ViewModel.MessageText = string.Empty;
    }
}
