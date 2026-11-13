global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using Cloud.Networking;
global using Moq;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Test;

internal static class TestData
{
    public static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Creates a Message. Change here if Message has a different shape.</summary>
    public static Message Msg(string content = "hello", DateTimeOffset? timestamp = null, Guid? id = null, string sender = "alice")
    {
        return new Message {
            Id = id ?? Guid.NewGuid(),
            SenderId = sender,
            Content = content,
            Timestamp = timestamp ?? T0
        };
    }
}

internal static class TestMessenger
{
    /// <summary>A messenger mock whose State follows ConnectAsync and DisconnectAsync.</summary>
    public static Mock<IMessenger> Create(
        Task? connectGate = null,
        Exception? connectError = null,
        Exception? sendError = null)
    {
        ConnectionState state = ConnectionState.Disconnected;
        Mock<IMessenger> mock = new();

        mock.SetupGet(m => m.State).Returns(() => state);
        mock.Setup(m => m.ConnectAsync()).Returns(async () => {
            if (connectGate is not null)
            {
                await connectGate;
            }

            if (connectError is not null)
            {
                throw connectError;
            }

            state = ConnectionState.Connected;
        });
        mock.Setup(m => m.DisconnectAsync()).Returns(() => {
            state = ConnectionState.Disconnected;
            return Task.CompletedTask;
        });
        mock.Setup(m => m.SendMessageAsync(It.IsAny<string>()))
            .Returns(sendError is null ? Task.CompletedTask : Task.FromException(sendError));
        mock.SyncReturns();
        return mock;
    }

    public static void SyncReturns(this Mock<IMessenger> mock, params Message[] messages)
    {
        mock.Setup(m => m.SyncAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync((IReadOnlyList<Message>)messages);
    }

    public static void SetState(this Mock<IMessenger> mock, ConnectionState state)
    {
        mock.SetupGet(m => m.State).Returns(state);
    }

    public static void RaiseMessage(this Mock<IMessenger> mock, Message message)
    {
        mock.Raise(m => m.MessageReceived += null, message);
    }

    public static void RaiseReconnecting(this Mock<IMessenger> mock, Exception? error = null)
    {
        mock.Raise(m => m.Reconnecting += null, error!);
    }

    public static void RaiseReconnected(this Mock<IMessenger> mock, string? id = "id")
    {
        mock.Raise(m => m.Reconnected += null, id!);
    }

    public static void RaiseClosed(this Mock<IMessenger> mock, Exception? error = null)
    {
        mock.Raise(m => m.Closed += null, error!);
    }
}

/// <summary>A view model wired to messenger mocks.</summary>
internal sealed class ViewModelHarness
{
    public const string Url = "https://localhost:5001/hub";

    /// <summary>Runs UI work immediately, so no dispatcher thread is needed.</summary>
    public static readonly Action<Action> RunNow = action => action();

    public ViewModelHarness()
    {
        ViewModel = new NotificationServiceViewModel(url => {
            Mock<IMessenger> mock = TestMessenger.Create();
            Messengers.Add(mock);
            return mock.Object;
        }, RunNow);
    }

    public NotificationServiceViewModel ViewModel { get; }
    public List<Mock<IMessenger>> Messengers { get; } = new();
    public Mock<IMessenger> Current => Messengers[^1];

    public async Task ConnectAsync(string url = Url)
    {
        ViewModel.ServerUrl = url;
        await ViewModel.ConnectCommand.ExecuteAsync();
    }
}
