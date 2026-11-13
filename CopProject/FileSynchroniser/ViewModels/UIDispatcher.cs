// Author : Murali

namespace FileSynchroniser.ViewModels;

/// <summary>
/// Marshals work onto the thread of an <see cref="IDispatcherQueue"/>.
/// </summary>
public sealed class UiDispatcher : IUiDispatcher
{
    private readonly IDispatcherQueue _queue;

    public UiDispatcher(IDispatcherQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);

        _queue = queue;
    }

    /// <summary>
    /// Runs inline when already on the UI thread; otherwise enqueues the action.
    /// </summary>
    public void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_queue.HasThreadAccess)
        {
            action();
        }
        else
        {
            _queue.TryEnqueue(action);
        }
    }

    /// <summary>
    /// Creates a dispatcher for the current UI thread, or one that runs inline
    /// when no WinUI runtime/dispatcher exists (e.g. unit tests).
    /// </summary>
    public static IUiDispatcher Create()
    {
        return Create(WinUiDispatcherQueue.TryCreate);
    }

    /// <summary>
    /// Creates a dispatcher from the given queue factory; falls back to inline
    /// execution when the factory returns null.
    /// </summary>
    public static IUiDispatcher Create(Func<IDispatcherQueue?> queueFactory)
    {
        ArgumentNullException.ThrowIfNull(queueFactory);

        IDispatcherQueue? queue = queueFactory();

        return queue is null ? new InlineUiDispatcher() : new UiDispatcher(queue);
    }

    /// <summary>
    /// Dispatcher that runs every action immediately on the calling thread.
    /// </summary>
    public sealed class InlineUiDispatcher : IUiDispatcher
    {
        public void Run(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            action();
        }
    }
}
