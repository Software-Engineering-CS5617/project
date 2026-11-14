using Cloud.UI.Commands;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Test;

[TestClass]
public class AsyncCommandBaseTests
{
    private sealed class TestCommand : AsyncCommandBase
    {
        public bool Allowed { get; set; } = true;
        public Func<Task> Body { get; set; } = () => Task.CompletedTask;
        public int Runs { get; private set; }

        protected override bool CanRun()
        {
            return Allowed;
        }

        protected override async Task OnExecuteAsync()
        {
            Runs++;
            await Body();
        }
    }

    [TestMethod]
    public void CanExecute_DefaultsToTrue()
    {
        Assert.IsTrue(new TestCommand().CanExecute(null));
    }

    [TestMethod]
    public void CanExecute_FollowsCanRun()
    {
        TestCommand command = new() { Allowed = false };

        Assert.IsFalse(command.CanExecute(null));
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenNotAllowed_DoesNothing()
    {
        TestCommand command = new() { Allowed = false };

        await command.ExecuteAsync();

        Assert.AreEqual(0, command.Runs);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenAllowed_RunsOnce()
    {
        TestCommand command = new();

        await command.ExecuteAsync();

        Assert.AreEqual(1, command.Runs);
        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod]
    public async Task WhileRunning_CannotExecute_AndSecondCallIsIgnored()
    {
        TaskCompletionSource gate = new();
        TestCommand command = new() { Body = () => gate.Task };

        Task first = command.ExecuteAsync();
        bool canExecuteWhileRunning = command.CanExecute(null);
        await command.ExecuteAsync(); // ignored

        Assert.IsFalse(canExecuteWhileRunning);
        Assert.AreEqual(1, command.Runs);

        gate.SetResult();
        await first;
        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod]
    public async Task ExecuteAsync_RaisesCanExecuteChanged_AtStartAndEnd()
    {
        TestCommand command = new();
        int raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        await command.ExecuteAsync();

        Assert.AreEqual(2, raised);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenBodyThrows_PropagatesAndResets()
    {
        TestCommand command = new() { Body = () => throw new InvalidOperationException("boom") };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(command.ExecuteAsync);

        Assert.IsTrue(command.CanExecute(null));
    }

}

[TestClass]
public class NotificationCommandTests
{
    [TestMethod]
    public async Task Command_Failure_IsShownAsError_NotThrown()
    {
        NotificationServiceViewModel vm = new(url => TestMessenger.Create(sendError: new InvalidOperationException("send failed")).Object, ViewModelHarness.RunNow) {
            ServerUrl = ViewModelHarness.Url
        };
        await vm.ConnectCommand.ExecuteAsync();
        vm.MessageText = "hi";

        await vm.SendCommand.ExecuteAsync();

        Assert.AreEqual("send failed", vm.ErrorText);
        Assert.AreEqual("hi", vm.MessageText, "the text is kept so the user can retry");
        Assert.AreEqual(ConnectionState.Connected, vm.State);
        Assert.IsTrue(vm.SendCommand.CanExecute(null), "busy flag must be released after a failure");
    }

    [TestMethod]
    public void Execute_ICommandEntryPoint_RunsTheCommand()
    {
        ViewModelHarness h = new();
        h.ViewModel.ServerUrl = ViewModelHarness.Url;

        h.ViewModel.ConnectCommand.Execute(null);

        Assert.AreEqual(1, h.Messengers.Count);
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
    }
}
