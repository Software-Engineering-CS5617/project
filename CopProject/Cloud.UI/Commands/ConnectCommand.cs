using Cloud.Networking;
using Cloud.UI.Services;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>
/// Connects to the server URL typed by the user, 
/// starting from a clean connection.
/// </summary>
public class ConnectCommand : NotificationCommand
{
    private const string InvalidUrlMessage = "Enter a full http:// or https:// URL, e.g. https://localhost:5001/hub";

    public ConnectCommand(NotificationServiceViewModel viewModel, NotificationService service)
        : base(viewModel, service)
    {
    }

    protected override bool IsAvailable()
    {
        return ViewModel.State == ConnectionState.Disconnected && ViewModel.ServerUrl.Trim().Length > 0;
    }

    protected override async Task RunAsync()
    {
        if (!NotificationService.TryParseUrl(ViewModel.ServerUrl, out string url))
        {
            ViewModel.ErrorText = InvalidUrlMessage;
            return;
        }

        await Service.UseServerAsync(url);

        ViewModel.State = ConnectionState.Connecting;
        await Service.ConnectAsync();

        // Reached only when the connection worked
        ViewModel.ServerUrl = url;
    }
}
