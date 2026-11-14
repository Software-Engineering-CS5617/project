using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>
/// Empties the notification list. Synchronous, so it inherits <see cref="CommandBase"/> directly.
/// </summary>
public sealed class ClearCommand : CommandBase
{
    private readonly NotificationServiceViewModel _viewModel;

    public ClearCommand(NotificationServiceViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
    }

    public override bool CanExecute(object? parameter)
    {
        return _viewModel.Notifications.Count > 0;
    }

    public override void Execute(object? parameter)
    {
        _viewModel.Notifications.Clear();
    }
}
