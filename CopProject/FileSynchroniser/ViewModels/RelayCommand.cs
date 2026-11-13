// Author : Murali

using System.Windows.Input;

namespace FileSynchroniser.ViewModels;

/// <summary>
/// Minimal ICommand implementation that runs a delegate.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Initializes a new command with the action to run and an optional condition for enabling it.
    /// </summary>
    /// <param name="execute">The action to run when the command is executed.</param>
    /// <param name="canExecute">Optional condition that decides whether the command can run.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Occurs when the result of <see cref="CanExecute"/> may have changed.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Determines whether the command can run in its current state.
    /// </summary>
    /// <returns><c>true</c> if no condition is set or the condition passes; otherwise <c>false</c>.</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    /// <summary>
    /// Runs the command's action.
    /// </summary>
    public void Execute(object? parameter)
    {
        _execute();
    }

    /// <summary>
    /// Notifies the UI to re-evaluate whether the command can run.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
