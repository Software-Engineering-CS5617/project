// Author : Murali

using System;
using FileSynchroniser.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Filesync.Test.ViewModels;

[TestClass]
public class RelayCommandTest
{
    private RelayCommand _command = null!;

    /// <summary>
    /// Initializing RelayCommand for testing command operations
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _command = new RelayCommand(() => { });
    }

    /// <summary>
    /// Clean up once test is completed
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _command = null!;
    }

    /// <summary>
    /// 1. Constructor throws ArgumentNullException when execute action is null
    /// </summary>
    [TestMethod]
    public void Constructor_NullExecute_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new RelayCommand(null!));
    }

    /// <summary>
    /// 2. Execute invokes the provided action
    /// </summary>
    [TestMethod]
    public void Execute_InvokesAction()
    {
        bool executed = false;
        RelayCommand command = new RelayCommand(() => executed = true);

        command.Execute(null);

        Assert.IsTrue(executed, "The execute action should have been invoked.");
    }

    /// <summary>
    /// 3. CanExecute returns true when no predicate is provided
    /// </summary>
    [TestMethod]
    public void CanExecute_NoPredicate_ReturnsTrue()
    {
        bool result = _command.CanExecute(null);

        Assert.IsTrue(result, "CanExecute should return true by default when no predicate is provided.");
    }

    /// <summary>
    /// 4. CanExecute returns true when the provided predicate returns true
    /// </summary>
    [TestMethod]
    public void CanExecute_WithPredicate_ReturnsTrue()
    {
        RelayCommand command = new RelayCommand(() => { }, () => true);

        bool result = command.CanExecute(null);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// 5. CanExecute returns false when the provided predicate returns false
    /// </summary>
    [TestMethod]
    public void CanExecute_WithPredicate_ReturnsFalse()
    {
        RelayCommand command = new RelayCommand(() => { }, () => false);

        bool result = command.CanExecute(null);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// 6. RaiseCanExecuteChanged invokes the CanExecuteChanged event
    /// </summary>
    [TestMethod]
    public void RaiseCanExecuteChanged_InvokesCanExecuteChangedEvent()
    {
        bool eventRaised = false;

        _command.CanExecuteChanged += (sender, args) => eventRaised = true;

        _command.RaiseCanExecuteChanged();

        Assert.IsTrue(eventRaised, "The CanExecuteChanged event should have been raised.");
    }

    /// <summary>
    /// 7. RaiseCanExecuteChanged with no subscribers
    /// </summary>
    [TestMethod]
    public void RaiseCanExecuteChanged_WithNoSubscribers_DoesNotThrow()
    {
        var command = new RelayCommand(() => { });

        command.RaiseCanExecuteChanged();
    }
}
