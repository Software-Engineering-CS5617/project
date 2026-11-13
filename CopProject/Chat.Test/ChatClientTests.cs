using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using Chat;
using Networking;

namespace Chat.Test;

public class ChatClientTests
{
    private static readonly IPEndPoint ServerAddress = new(IPAddress.Loopback, 12345);

    [Fact]
    public void ConstructorRejectsInvalidUsername()
    {
        RecordingCommunicator communicator = new();
        Chatroom chatroom = new("room", "host");

        Assert.ThrowsAny<ArgumentException>(
            () => new ChatClient(" ", chatroom, communicator, ServerAddress));
    }

    [Fact]
    public void ConstructorRejectsNullChatroom()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ChatClient("guest", null!, new RecordingCommunicator(), ServerAddress));
    }

    [Fact]
    public void ConstructorRejectsNullCommunicator()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ChatClient("guest", new Chatroom("room", "host"), null!, ServerAddress));
    }

    [Fact]
    public void ConstructorRejectsNullServerAddress()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ChatClient("guest", new Chatroom("room", "host"), new RecordingCommunicator(), null!));
    }

    [Fact]
    public void ConstructorRegistersClientAsCommunicatorSubscriber()
    {
        RecordingCommunicator communicator = new();
        Chatroom chatroom = new("room", "host");

        ChatClient client = new("guest", chatroom, communicator, ServerAddress);

        Assert.Equal("guest", client.Username);
        Assert.Same(chatroom, client.Chatroom);
        Assert.Equal(ChatConstants.ModuleId, communicator.SubscribedModule);
        Assert.Same(client, communicator.Subscriber);
        client.Dispose();
    }

    [Fact]
    public void SyncSendsSyncRequestEnvelope()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);

        Guid requestId = client.Sync();

        AssertRequest<Sync>(communicator.LastSentMessage!, requestId);
        AssertSentToServer(communicator);
    }

    [Fact]
    public void ResetSendsResetRequestEnvelope()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);

        Guid requestId = client.Reset();

        AssertRequest<Reset>(communicator.LastSentMessage!, requestId);
        AssertSentToServer(communicator);
    }

    [Fact]
    public void SendMessageSendsContentMentionsAndThreadParent()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);
        Guid parentId = Guid.NewGuid();
        Mentions mentions = new();
        mentions.Usernames.Add("host");

        Guid requestId = client.SendMessage("Hello", mentions, parentId);

        ChatClientRequestEnvelope envelope = AssertRequest<SendMessage>(communicator.LastSentMessage!, requestId);
        SendMessage send = Assert.IsType<SendMessage>(envelope.Request);
        Assert.Equal("Hello", send.Content);
        Assert.Equal(parentId, send.ThreadParentMessageId);
        using JsonDocument payload = JsonDocument.Parse(communicator.LastSentMessage!);
        JsonElement usernames = payload.RootElement
            .GetProperty("Request")
            .GetProperty("Mentions")
            .GetProperty("Usernames");
        Assert.Contains(usernames.EnumerateArray(), username => username.GetString() == "host");
        AssertSentToServer(communicator);
    }

    [Fact]
    public void SendMessageSendsInlineReplyIdSeparatelyFromThreadParent()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);
        Guid threadParentId = Guid.NewGuid();
        Guid inlineReplyId = Guid.NewGuid();

        Guid requestId = client.SendMessage("Hello", new Mentions(), threadParentId, inlineReplyId);

        SendMessage send = Assert.IsType<SendMessage>(
            AssertRequest<SendMessage>(communicator.LastSentMessage!, requestId).Request);
        Assert.Equal(threadParentId, send.ThreadParentMessageId);
        Assert.Equal(inlineReplyId, send.ReplyToMessageId);
    }

    [Fact]
    public void SendMessageSetsTimestamp()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);

        DateTimeOffset sentBefore = DateTimeOffset.UtcNow;
        Guid requestId = client.SendMessage("Hello", new Mentions());
        DateTimeOffset sentAfter = DateTimeOffset.UtcNow;

        ChatClientRequestEnvelope envelope = AssertRequest<SendMessage>(communicator.LastSentMessage!, requestId);
        SendMessage send = Assert.IsType<SendMessage>(envelope.Request);
        Assert.InRange(send.Timestamp, sentBefore, sentAfter);
    }

    [Fact]
    public void EditMessageSendsMessageContentAndThreadParent()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);
        Guid messageId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();

        Guid requestId = client.EditMessage(messageId, "Edited", parentId);

        ChatClientRequestEnvelope envelope = AssertRequest<EditMessage>(communicator.LastSentMessage!, requestId);
        EditMessage edit = Assert.IsType<EditMessage>(envelope.Request);
        Assert.Equal(messageId, edit.MessageId);
        Assert.Equal("Edited", edit.NewContent);
        Assert.Equal(parentId, edit.ThreadParentMessageId);
        AssertSentToServer(communicator);
    }

    [Fact]
    public void DeleteMessageSendsMessageAndThreadParent()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);
        Guid messageId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();

        Guid requestId = client.DeleteMessage(messageId, parentId);

        ChatClientRequestEnvelope envelope = AssertRequest<DeleteMessage>(communicator.LastSentMessage!, requestId);
        DeleteMessage delete = Assert.IsType<DeleteMessage>(envelope.Request);
        Assert.Equal(messageId, delete.MessageId);
        Assert.Equal(parentId, delete.ThreadParentMessageId);
        AssertSentToServer(communicator);
    }

    [Fact]
    public void ToggleReactionSendsEmojiAndThreadParent()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);
        Guid messageId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();

        Guid requestId = client.ToggleReaction(messageId, "👍", parentId);

        ChatClientRequestEnvelope envelope = AssertRequest<ToggleReaction>(communicator.LastSentMessage!, requestId);
        ToggleReaction reaction = Assert.IsType<ToggleReaction>(envelope.Request);
        Assert.Equal(messageId, reaction.MessageId);
        Assert.Equal("👍", reaction.Emoji);
        Assert.Equal(parentId, reaction.ThreadParentMessageId);
        AssertSentToServer(communicator);
    }

    [Fact]
    public void SendMessageRejectsNullContent()
    {
        using ChatClient client = NewClient(new RecordingCommunicator());

        Assert.Throws<ArgumentNullException>(() => client.SendMessage(null!, new Mentions()));
    }

    [Fact]
    public void SendMessageRejectsNullMentions()
    {
        using ChatClient client = NewClient(new RecordingCommunicator());

        Assert.Throws<ArgumentNullException>(() => client.SendMessage("Hello", null!));
    }

    [Fact]
    public void EditMessageRejectsNullContent()
    {
        using ChatClient client = NewClient(new RecordingCommunicator());

        Assert.Throws<ArgumentNullException>(() => client.EditMessage(Guid.NewGuid(), null!));
    }

    [Fact]
    public void ToggleReactionRejectsWhitespaceEmoji()
    {
        using ChatClient client = NewClient(new RecordingCommunicator());

        Assert.ThrowsAny<ArgumentException>(() => client.ToggleReaction(Guid.NewGuid(), " "));
    }

    [Fact]
    public void DisposeUnsubscribesFromCommunicator()
    {
        RecordingCommunicator communicator = new();
        ChatClient client = NewClient(communicator);

        client.Dispose();

        Assert.Equal(1, communicator.RemoveSubscriberCount);
        Assert.Equal(ChatConstants.ModuleId, communicator.RemovedModule);
    }

    [Fact]
    public void DisposeCanBeCalledMoreThanOnce()
    {
        RecordingCommunicator communicator = new();
        ChatClient client = NewClient(communicator);

        client.Dispose();
        client.Dispose();

        Assert.Equal(1, communicator.RemoveSubscriberCount);
    }

    [Fact]
    public void RequestsAfterDisposeThrowObjectDisposedException()
    {
        ChatClient client = NewClient(new RecordingCommunicator());

        client.Dispose();

        Assert.Throws<ObjectDisposedException>(() => client.Sync());
    }

    [Fact]
    public void AppliesBatchRaisesEventsAdvancesVersionAndUpdatesParticipants()
    {
        Chatroom chatroom = new("room", "host");
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator, chatroom);
        Guid messageId = Guid.NewGuid();
        ChatOperation operation = new MessageAdded(messageId, NewMessage("guest", "Hello"));
        List<ChatOperation> applied = new();
        client.OperationApplied += applied.Add;
        ChatServerUpdateEnvelope update = new(
            new ChatServerOperationBatch(0, 1, new[] { operation }),
            new[] { "guest", "host" });

        client.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes<ChatServerMessage>(update));

        Assert.Equal(1, client.Version);
        MessageAdded appliedOperation = Assert.IsType<MessageAdded>(Assert.Single(applied));
        Assert.Equal(messageId, appliedOperation.MessageId);
        Assert.True(chatroom.TryGetMessage(messageId, out ChatMessage message));
        Assert.Equal("Hello", message.Content);
        Assert.Contains("guest", chatroom.Participants);
    }

    [Fact]
    public void ResetClearsMessagesAndResetsVersion()
    {
        Chatroom chatroom = new("room", "host");
        Guid existingId = chatroom.AddMessage(NewMessage("guest", "Old"));
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator, chatroom);
        ApplyBatch(client, 0, 1, new ChatOperation[]
        {
            new MessageEdited(existingId, "Updated", "guest", null)
        });
        Assert.Equal(1, client.Version);

        ChatServerUpdateEnvelope update = new(new ChatServerReset(), new[] { "host" });
        client.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes<ChatServerMessage>(update));

        Assert.Equal(0, client.Version);
        Assert.False(chatroom.TryGetMessage(existingId, out _));
        Assert.DoesNotContain("guest", chatroom.Participants);
    }

    [Fact]
    public void RequestResultRaisesCompletionEvent()
    {
        using ChatClient client = NewClient(new RecordingCommunicator());
        Guid requestId = Guid.NewGuid();
        (Guid Id, RoomResult Result)? completed = null;
        client.RequestCompleted += (id, result) => completed = (id, result);

        ChatServerMessage result = new ChatServerRequestResult(requestId, RoomResult.Forbidden);
        client.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes(result));

        Assert.Equal((requestId, RoomResult.Forbidden), completed);
    }

    [Fact]
    public void BatchWithUnexpectedStartingVersionRequestsReset()
    {
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator);

        ApplyBatch(client, 1, 1, Array.Empty<ChatOperation>());

        AssertRequest<Reset>(communicator.LastSentMessage!);
        Assert.Equal(0, client.Version);
    }

    [Fact]
    public void BatchWithUnexpectedEndingVersionRequestsResetAfterApplyingOperations()
    {
        Chatroom chatroom = new("room", "host");
        RecordingCommunicator communicator = new();
        using ChatClient client = NewClient(communicator, chatroom);
        Guid messageId = Guid.NewGuid();
        ChatOperation operation = new MessageAdded(messageId, NewMessage("guest", "Hello"));

        ApplyBatch(client, 0, 2, new[] { operation });

        AssertRequest<Reset>(communicator.LastSentMessage!);
        Assert.Equal(1, client.Version);
        Assert.True(chatroom.TryGetMessage(messageId, out _));
    }

    [Fact]
    public void BatchWithUnapplicableOperationThrowsInvalidDataException()
    {
        using ChatClient client = NewClient(new RecordingCommunicator());
        ChatOperation operation = new MessageEdited(Guid.NewGuid(), "Edited", "guest", null);
        ChatServerUpdateEnvelope update = new(
            new ChatServerOperationBatch(0, 1, new[] { operation }),
            Array.Empty<string>());

        Assert.Throws<InvalidDataException>(
            () => client.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes<ChatServerMessage>(update)));
        Assert.Equal(0, client.Version);
    }

    [Fact]
    public void OnMessageReceivedRejectsNullAndEmptyPayload()
    {
        using ChatClient client = NewClient(new RecordingCommunicator());

        Assert.Throws<ArgumentNullException>(() => client.OnMessageReceived(null!));
        Assert.Throws<InvalidDataException>(() => client.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes<ChatServerMessage?>(null)));
    }

    [Fact]
    public void OnMessageReceivedRejectsMalformedJson()
    {
        using ChatClient client = NewClient(new RecordingCommunicator());

        Assert.Throws<JsonException>(() => client.OnMessageReceived(new byte[] { (byte)'{' }));
    }

    private static ChatClient NewClient(RecordingCommunicator communicator, Chatroom? chatroom = null)
    {
        return new ChatClient(
            "guest",
            chatroom ?? new Chatroom("room", "host"),
            communicator,
            ServerAddress);
    }

    private static ChatMessageWithThreads NewMessage(string sender, string content)
    {
        return new ChatMessageWithThreads { SenderName = sender, Content = content };
    }

    private static void AssertSentToServer(RecordingCommunicator communicator)
    {
        Assert.Equal(ServerAddress, communicator.LastDestination);
        Assert.Equal(ChatConstants.ModuleId, communicator.LastSentModule);
        Assert.Equal(1, communicator.LastPriority);
    }

    private static void ApplyBatch(
        ChatClient client,
        int fromVersion,
        int toVersion,
        IReadOnlyList<ChatOperation> operations)
    {
        ChatServerUpdateEnvelope update = new(
            new ChatServerOperationBatch(fromVersion, toVersion, operations),
            Array.Empty<string>());
        client.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes<ChatServerMessage>(update));
    }

    private static ChatClientRequestEnvelope AssertRequest<TRequest>(byte[] payload, Guid? expectedId = null)
        where TRequest : ChatClientRequest
    {
        ChatClientRequestEnvelope envelope = JsonSerializer.Deserialize<ChatClientRequestEnvelope>(payload)!;
        Assert.IsType<TRequest>(envelope.Request);
        Assert.Equal("guest", envelope.Username);
        Assert.NotEqual(Guid.Empty, envelope.RequestId);
        if (expectedId is Guid id)
        {
            Assert.Equal(id, envelope.RequestId);
        }

        return envelope;
    }

    private sealed class RecordingCommunicator : ICommunicator
    {
        public int ListenPort => 0;
        public IPAddress ListenIpAddress => IPAddress.Loopback;
        public int? SubscribedModule { get; private set; }
        public IMessageListener? Subscriber { get; private set; }
        public int RemoveSubscriberCount { get; private set; }
        public int? RemovedModule { get; private set; }
        public byte[]? LastSentMessage { get; private set; }
        public IPEndPoint? LastDestination { get; private set; }
        public int? LastSentModule { get; private set; }
        public int? LastPriority { get; private set; }

        public void AddSubscriber(int module, IMessageListener subscriber)
        {
            SubscribedModule = module;
            Subscriber = subscriber;
        }

        public void RemoveSubscriber(int module)
        {
            RemoveSubscriberCount++;
            RemovedModule = module;
        }

        public void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
        {
            LastDestination = iPEndPoint;
            LastSentMessage = message;
            LastSentModule = module;
            LastPriority = priority;
        }

        public void BroadcastMessage(byte[] message, int module, int priority)
        {
        }

        public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp)
        {
        }

        public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp)
        {
        }

        public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
        {
        }
    }
}
