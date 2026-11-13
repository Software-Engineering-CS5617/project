
using System;
using System.Windows.Input;

namespace Incidents.ViewModels.Utils;

/// <summary>
/// Simple ICommand that runs an action when it is executed.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;

    /// <summary>
    /// Creates the command with the action to run, and throws if it is null.
    /// </summary>
    public RelayCommand(Action execute)
    {
        _execute = execute
            ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// Required by ICommand but never raised because the command is always enabled.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Always returns true so the command can run at any time.
    /// </summary>
    /// <param name="parameter">Unused, only needed for the ICommand interface.</param>
    public bool CanExecute(object? parameter)
    {
        return true;
    }

    /// <summary>
    /// Runs the action given in the constructor.
    /// </summary>
    /// <param name="parameter">Unused, only needed for the ICommand interface.</param>
    public void Execute(object? parameter)
    {
        _execute();
    }
}
