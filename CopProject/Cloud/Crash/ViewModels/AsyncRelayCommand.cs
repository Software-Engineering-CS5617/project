using System.Windows.Input;

namespace Cloud.Crash.ViewModels;

/// <summary>
/// A command that runs an async method and is disabled while it runs.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isRunning;

    /// <summary>
    /// Creates the command.
    /// </summary>
    /// <param name="execute">The async method to run.</param>
    /// <param name="canExecute">Tells whether the command is enabled; always enabled if null.</param>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return !_isRunning && (_canExecute?.Invoke() ?? true);
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        _ = ExecuteAsync();
    }

    /// <summary>
    /// Runs the command and completes when it is done. Does nothing while it is already running.
    /// </summary>
    /// <returns>A task that completes when the command is done.</returns>
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
            await _execute();
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Tells the UI to ask <see cref="CanExecute"/> again.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
