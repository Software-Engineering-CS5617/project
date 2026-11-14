namespace Cloud.UI.Commands;

/// <summary>
/// Base of all async commands. Cannot be started again while it is running.
/// </summary>
public abstract class AsyncCommandBase : CommandBase
{
    private bool _isRunning;

    public sealed override bool CanExecute(object? parameter)
    {
        return !_isRunning && CanRun();
    }

    public sealed override async void Execute(object? parameter)
    {
        await ExecuteAsync();
    }

    /// <summary>Runs the command if it can run now. does nothing otherwise.</summary>
    public async Task ExecuteAsync()
    {
        if (!CanExecute(null))
        {
            return;
        }

        _isRunning = true;
        RaiseCanExecuteChanged();
        try
        {
            await OnExecuteAsync();
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>Extra condition for running. Defualt: True</summary>
    protected virtual bool CanRun()
    {
        return true;
    }

    // something the inherited classes can only use
    protected abstract Task OnExecuteAsync();
}
