using Cloud.UI.Services;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>
/// Does exactly what <see cref="ConnectCommand"/> does; the difference is only
/// that it is available while online (connected or reconnecting).
/// </summary>
public sealed class ReconnectCommand : ConnectCommand
{
    public ReconnectCommand(NotificationServiceViewModel viewModel, NotificationService service)
        : base(viewModel, service)
    {
    }

    protected override bool IsAvailable()
    {
        return ViewModel.IsOnline;
    }
}
