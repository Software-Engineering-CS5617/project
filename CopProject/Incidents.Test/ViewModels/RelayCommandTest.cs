using System;
using Incidents.ViewModels.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Incidents.Test.ViewModels;

/// <summary>
/// Testing RelayCommand functionality.
/// </summary>
[TestClass]
public sealed class RelayCommandTest
{
    /// <summary>
    /// Command executes the supplied action.
    /// </summary>
    [TestMethod]
    public void Execute_RunsSuppliedAction()
    {
        bool executed = false;
        RelayCommand command = new(() => executed = true);
        command.Execute(null);

        Assert.IsTrue(executed);
    }

    /// <summary>
    /// Command can always execute.
    /// </summary>
    [TestMethod]
    public void CanExecute_AlwaysReturnsTrue()
    {
        RelayCommand command = new(() => { });

        Assert.IsTrue(command.CanExecute(null));
    }

    /// <summary>
    /// Command rejects a null action.
    /// </summary>
    [TestMethod]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
    }

    /// <summary>
    /// Command executes the action only when Execute is called.
    /// </summary>
    [TestMethod]
    public void Constructor_DoesNotExecuteActionImmediately()
    {
        int executionCount = 0;
        RelayCommand command = new(() => executionCount++);

        Assert.AreEqual(0, executionCount);

        command.Execute(null);

        Assert.AreEqual(1, executionCount);
    }

    /// <summary>
    /// Command can execute multiple times.
    /// </summary>
    [TestMethod]
    public void Execute_MultipleCalls_ExecutesActionMultipleTimes()
    {
        int executionCount = 0;
        RelayCommand command = new(() => executionCount++);

        command.Execute(null);
        command.Execute(null);
        command.Execute(null);

        Assert.AreEqual(3, executionCount);
    }
}
