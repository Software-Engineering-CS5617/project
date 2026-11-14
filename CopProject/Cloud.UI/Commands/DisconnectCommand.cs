using Cloud.UI.Services;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>Closes the connection.</summary>
public sealed class DisconnectCommand : NotificationCommand
{
    public DisconnectCommand(NotificationServiceViewModel viewModel, NotificationService service)
        : base(viewModel, service)
    {
    }

    protected override bool IsAvailable()
    {
        return ViewModel.IsOnline;
    }

    protected override Task RunAsync()
    {
        return Service.DisconnectAsync();
    }
}
