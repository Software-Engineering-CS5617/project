// Author : Murali

namespace FileSynchroniser.ViewModels;

/// <summary>
/// Minimal abstraction over a UI thread queue (WinUI's DispatcherQueue is sealed and
/// cannot be mocked, so the logic in <see cref="UiDispatcher"/> talks to this instead).
/// </summary>
public interface IDispatcherQueue
{
    /// <summary>
    /// True when the calling thread is the thread this queue belongs to.
    /// </summary>
    bool HasThreadAccess { get; }

    /// <summary>
    /// Schedules the action on the queue's thread.
    /// </summary>
    /// <returns><c>true</c> if the action was queued.</returns>
    bool TryEnqueue(Action action);
}
