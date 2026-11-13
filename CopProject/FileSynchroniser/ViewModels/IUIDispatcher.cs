// Author : Murali

namespace FileSynchroniser.ViewModels;

/// <summary>
/// Runs work on the UI thread.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// Runs the action on the UI thread (inline if already on it).
    /// </summary>
    void Run(Action action);
}
