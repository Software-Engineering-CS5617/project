// Author : Murali

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace FileSynchroniser.ViewModels;

/// <summary>
/// Adapter over WinUI's DispatcherQueue. It is the only
/// piece that needs a real WinUI runtime and is excluded from unit-test coverage.
/// </summary>
internal sealed class WinUiDispatcherQueue : IDispatcherQueue
{
    private readonly DispatcherQueue _queue;

    private WinUiDispatcherQueue(DispatcherQueue queue)
    {
        _queue = queue;
    }

    public bool HasThreadAccess => _queue.HasThreadAccess;

    public bool TryEnqueue(Action action)
    {
        return _queue.TryEnqueue(() => action());
    }

    /// <summary>
    /// Wraps the current thread's DispatcherQueue, or returns null when unavailable.
    /// </summary>
    public static IDispatcherQueue? TryCreate()
    {
        try
        {
            DispatcherQueue? queue = DispatcherQueue.GetForCurrentThread();

            return queue is null ? null : new WinUiDispatcherQueue(queue);
        }
        catch (COMException)
        {
            // "Class not registered": the Windows App SDK runtime isn't available.
            return null;
        }
    }
}
