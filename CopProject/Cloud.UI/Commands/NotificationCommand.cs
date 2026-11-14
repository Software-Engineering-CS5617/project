using Cloud.UI.Services;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>
/// Base of the commands of the Notification Service page. 
/// </summary>
public abstract class NotificationCommand : AsyncCommandBase
{
    protected NotificationCommand(NotificationServiceViewModel viewModel, NotificationService service)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(service);
        ViewModel = viewModel;
        Service = service;
    }

    protected NotificationServiceViewModel ViewModel { get; }
    protected NotificationService Service { get; }


    /// <summary>When this command is available, not counting the busy check.</summary>
    protected abstract bool IsAvailable();

    /// <summary>The actual work of the command.</summary>
    protected abstract Task RunAsync();

    protected sealed override bool CanRun()
    {
        return !ViewModel.IsBusy && IsAvailable();
    }

    /// <summary>
    /// Runs one user operation: marks the view model busy, clears the previous error,
    /// shows any exception as an error and re-reads the real connection state at the end.
    /// </summary>
    protected sealed override async Task OnExecuteAsync()
    {
        ViewModel.IsBusy = true;
        ViewModel.ErrorText = null;
        try
        {
            await RunAsync();
        }
        catch (Exception ex)
        {
            ViewModel.ErrorText = ex.Message;
        }
        finally
        {
            ViewModel.IsBusy = false;
            ViewModel.State = Service.State;
        }
    }
}
