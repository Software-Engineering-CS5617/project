// Author : Murali

using System;
using System.Timers;
using FileSynchroniser.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static FileSynchroniser.ViewModels.UiDispatcher;

namespace Filesync.Test.ViewModels;

[TestClass]
public sealed class UiDispatcherTest
{
    private Mock<IDispatcherQueue> _queue = null!;
    private UiDispatcher _dispatcher = null!;

    /// <summary>
    /// Initializing a mocked dispatcher queue and the UiDispatcher under test
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _queue = new Mock<IDispatcherQueue>();
        _dispatcher = new UiDispatcher(_queue.Object);
    }

    /// <summary>
    /// 1. Constructor throws ArgumentNullException when queue is null
    /// </summary>
    [TestMethod]
    public void Constructor_NullQueue_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new UiDispatcher(null!));
    }

    /// <summary>
    /// 2. Run throws ArgumentNullException when action is null
    /// </summary>
    [TestMethod]
    public void Run_NullAction_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _dispatcher.Run(null!));
    }

    /// <summary>
    /// 3. Run executes the action inline when already on the UI thread
    /// </summary>
    [TestMethod]
    public void Run_HasThreadAccess_ExecutesInline()
    {
        _queue.Setup(q => q.HasThreadAccess).Returns(true);
        bool executed = false;

        _dispatcher.Run(() => executed = true);

        Assert.IsTrue(executed);
    }

    /// <summary>
    /// 4. Run does not enqueue when already on the UI thread
    /// </summary>
    [TestMethod]
    public void Run_HasThreadAccess_DoesNotEnqueue()
    {
        _queue.Setup(q => q.HasThreadAccess).Returns(true);

        _dispatcher.Run(() => { });

        _queue.Verify(q => q.TryEnqueue(It.IsAny<Action>()), Times.Never);
    }

    /// <summary>
    /// 5. Run enqueues the action when called from another thread
    /// </summary>
    [TestMethod]
    public void Run_NoThreadAccess_EnqueuesAction()
    {
        _queue.Setup(q => q.HasThreadAccess).Returns(false);
        _queue.Setup(q => q.TryEnqueue(It.IsAny<Action>())).Returns(true);

        _dispatcher.Run(() => { });

        _queue.Verify(q => q.TryEnqueue(It.IsAny<Action>()), Times.Once);
    }

    /// <summary>
    /// 6. Run does not execute the action immediately when called from another thread
    /// </summary>
    [TestMethod]
    public void Run_NoThreadAccess_DoesNotExecuteInline()
    {
        _queue.Setup(q => q.HasThreadAccess).Returns(false);
        _queue.Setup(q => q.TryEnqueue(It.IsAny<Action>())).Returns(true);
        bool executed = false;

        _dispatcher.Run(() => executed = true);

        Assert.IsFalse(executed);
    }

    /// <summary>
    /// 7. The enqueued action runs the original work once the queue invokes it
    /// </summary>
    [TestMethod]
    public void Run_NoThreadAccess_EnqueuedActionExecutesWork()
    {
        Action? captured = null;
        _queue.Setup(q => q.HasThreadAccess).Returns(false);
        _queue.Setup(q => q.TryEnqueue(It.IsAny<Action>()))
              .Callback<Action>(a => captured = a)
              .Returns(true);
        bool executed = false;

        _dispatcher.Run(() => executed = true);
        captured!.Invoke();

        Assert.IsTrue(executed);
    }

    /// <summary>
    /// 8. InlineUiDispatcher executes the action immediately
    /// </summary>
    [TestMethod]
    public void InlineUiDispatcher_Run_ExecutesImmediately()
    {
        bool executed = false;

        new InlineUiDispatcher().Run(() => executed = true);

        Assert.IsTrue(executed);
    }

    /// <summary>
    /// 9. InlineUiDispatcher throws ArgumentNullException when action is null
    /// </summary>
    [TestMethod]
    public void InlineUiDispatcher_NullAction_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new InlineUiDispatcher().Run(null!));
    }

    /// <summary>
    /// 10. Create throws ArgumentNullException when the queue factory is null
    /// </summary>
    [TestMethod]
    public void Create_NullFactory_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => UiDispatcher.Create(null!));
    }

    /// <summary>
    /// 11. Create returns an inline dispatcher when no queue is available
    /// </summary>
    [TestMethod]
    public void Create_FactoryReturnsNull_ReturnsInlineDispatcher()
    {
        IUiDispatcher result = UiDispatcher.Create(() => null);

        Assert.IsInstanceOfType<InlineUiDispatcher>(result);
    }

    /// <summary>
    /// 12. Create returns a UiDispatcher when a queue is available
    /// </summary>
    [TestMethod]
    public void Create_FactoryReturnsQueue_ReturnsUiDispatcher()
    {
        IUiDispatcher result = UiDispatcher.Create(() => _queue.Object);

        Assert.IsInstanceOfType<UiDispatcher>(result);
    }

    /// <summary>
    /// 13. The dispatcher created from a queue uses that queue
    /// </summary>
    [TestMethod]
    public void Create_FactoryReturnsQueue_DispatcherUsesQueue()
    {
        _queue.Setup(q => q.HasThreadAccess).Returns(false);
        _queue.Setup(q => q.TryEnqueue(It.IsAny<Action>())).Returns(true);

        UiDispatcher.Create(() => _queue.Object).Run(() => { });

        _queue.Verify(q => q.TryEnqueue(It.IsAny<Action>()), Times.Once);
    }

    /// <summary>
    /// 14. Parameterless Create always returns a usable dispatcher (inline when no WinUI runtime exists)
    /// </summary>
    [TestMethod]
    public void Create_Default_ReturnsDispatcher()
    {
        Assert.IsNotNull(UiDispatcher.Create());
    }
}
