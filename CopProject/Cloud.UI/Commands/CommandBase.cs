using System.Windows.Input;

namespace Cloud.UI.Commands;

/// <summary>
/// Root of all commands: implements the <see cref="CanExecuteChanged"/>
/// </summary>
public abstract class CommandBase : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public abstract bool CanExecute(object? parameter);

    public abstract void Execute(object? parameter);

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
