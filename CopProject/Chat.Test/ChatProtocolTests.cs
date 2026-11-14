using System;
using Chat;

namespace Chat.Test;

public class ChatProtocolTests
{
    [Fact]
    public void ApplyOperationRejectsNullChatroom()
    {
        Assert.Throws<ArgumentNullException>(
            () => ChatProtocol.ApplyOperation(null!, new UnknownOperation()));
    }

    [Fact]
    public void ApplyOperationRejectsNullOperation()
    {
        Assert.Throws<ArgumentNullException>(
            () => ChatProtocol.ApplyOperation(new Chatroom("room", "host"), null!));
    }

    [Fact]
    public void ApplyMessageAddedStoresIndependentMessageUsingSuppliedId()
    {
        Chatroom chatroom = new("room", "host");
        Guid messageId = Guid.NewGuid();
        ChatMessageWithThreads message = NewMessage("guest", "Hello");
        message.ToggleReaction("👍", "host");
        message.AddMessageToThread(Guid.NewGuid(), new ChatMessage { SenderName = "host" });

        RoomResult result = ChatProtocol.ApplyOperation(chatroom, new MessageAdded(messageId, message));

        Assert.Equal(RoomResult.Ok, result);
        Assert.True(chatroom.TryGetMessage(messageId, out ChatMessage stored));
        Assert.NotSame(message, stored);
        Assert.Equal("Hello", stored.Content);
        Assert.Contains("host", stored.Reactions["👍"]);
        Assert.Empty(((ChatMessageWithThreads)stored).ThreadById);
        Assert.Single(message.ThreadById);
    }

    [Fact]
    public void ApplyMessageAddedAddsReplyToExistingThreadUsingSuppliedId()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));
        Guid replyId = Guid.NewGuid();
        ChatMessageWithThreads reply = NewMessage("host", "Reply");
        RoomResult result = ChatProtocol.ApplyOperation(
            chatroom,
            new MessageAdded(replyId, reply, parentId));

        Assert.Equal(RoomResult.Ok, result);
        Assert.True(chatroom.TryGetMessage(replyId, out ChatMessage storedReply));
        Assert.Equal("Reply", storedReply.Content);
        Assert.Null(storedReply.ParentMessageId);
    }

    [Fact]
    public void ApplyMessageAddedReturnsNotFoundWhenThreadParentIsMissing()
    {
        Chatroom chatroom = new("room", "host");
        ChatMessageWithThreads reply = NewMessage("host", "Reply");
        Guid threadParentId = Guid.NewGuid();

        Assert.Equal(
            RoomResult.NotFound,
            ChatProtocol.ApplyOperation(chatroom, new MessageAdded(Guid.NewGuid(), reply, threadParentId)));
    }

    [Fact]
    public void ApplyMessageEditedUpdatesRoomMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid messageId = chatroom.AddMessage(NewMessage("guest", "Before"));

        RoomResult result = ChatProtocol.ApplyOperation(
            chatroom,
            new MessageEdited(messageId, "After", "guest", null));

        Assert.Equal(RoomResult.Ok, result);
        Assert.True(chatroom.TryGetMessage(messageId, out ChatMessage message));
        Assert.Equal("After", message.Content);
    }

    [Fact]
    public void ApplyMessageEditedReturnsNotFoundForMissingMessage()
    {
        Assert.Equal(
            RoomResult.NotFound,
            ChatProtocol.ApplyOperation(
                new Chatroom("room", "host"),
                new MessageEdited(Guid.NewGuid(), "After", "guest", null)));
    }

    [Fact]
    public void ApplyMessageDeletedMarksRoomMessageDeleted()
    {
        Chatroom chatroom = new("room", "host");
        Guid messageId = chatroom.AddMessage(NewMessage("guest", "Hello"));

        RoomResult result = ChatProtocol.ApplyOperation(
            chatroom,
            new MessageDeleted(messageId, "guest", null));

        Assert.Equal(RoomResult.Ok, result);
        Assert.True(chatroom.TryGetMessage(messageId, out ChatMessage message));
        Assert.True(message.IsDeleted);
    }

    [Fact]
    public void ApplyMessageDeletedReturnsNotFoundForMissingMessage()
    {
        Assert.Equal(
            RoomResult.NotFound,
            ChatProtocol.ApplyOperation(
                new Chatroom("room", "host"),
                new MessageDeleted(Guid.NewGuid(), "guest", null)));
    }

    [Fact]
    public void ApplyReactionToggledAddsReactionToRoomMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid messageId = chatroom.AddMessage(NewMessage("guest", "Hello"));

        RoomResult result = ChatProtocol.ApplyOperation(
            chatroom,
            new ReactionToggled(messageId, "👍", "host", null));

        Assert.Equal(RoomResult.Ok, result);
        Assert.True(chatroom.TryGetMessage(messageId, out ChatMessage message));
        Assert.Contains("host", message.Reactions["👍"]);
    }

    [Fact]
    public void ApplyReactionToggledReturnsNotFoundForMissingMessage()
    {
        Assert.Equal(
            RoomResult.NotFound,
            ChatProtocol.ApplyOperation(
                new Chatroom("room", "host"),
                new ReactionToggled(Guid.NewGuid(), "👍", "guest", null)));
    }

    [Fact]
    public void ApplyMessageEditedUpdatesReplyInSpecifiedThread()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));
        Guid replyId = chatroom.AddThreadMessage(parentId, new ChatMessage { SenderName = "host" });

        Assert.Equal(
            RoomResult.Ok,
            ChatProtocol.ApplyOperation(
                chatroom,
                new MessageEdited(replyId, "Edited", "host", parentId)));
        Assert.True(chatroom.TryGetMessage(replyId, out ChatMessage reply));
        Assert.Equal("Edited", reply.Content);
    }

    [Fact]
    public void ApplyReactionToggledUpdatesReplyInSpecifiedThread()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));
        Guid replyId = chatroom.AddThreadMessage(parentId, new ChatMessage { SenderName = "host" });

        Assert.Equal(
            RoomResult.Ok,
            ChatProtocol.ApplyOperation(
                chatroom,
                new ReactionToggled(replyId, "👍", "guest", parentId)));
        Assert.True(chatroom.TryGetMessage(replyId, out ChatMessage reply));
        Assert.Contains("guest", reply.Reactions["👍"]);
    }

    [Fact]
    public void ApplyMessageDeletedDeletesReplyInSpecifiedThread()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));
        Guid replyId = chatroom.AddThreadMessage(parentId, new ChatMessage { SenderName = "host" });

        Assert.Equal(
            RoomResult.Ok,
            ChatProtocol.ApplyOperation(
                chatroom,
                new MessageDeleted(replyId, "host", parentId)));
        Assert.True(chatroom.TryGetMessage(replyId, out ChatMessage reply));
        Assert.True(reply.IsDeleted);
    }

    [Fact]
    public void ApplyThreadOperationReturnsNotFoundForMissingParent()
    {
        Assert.Equal(
            RoomResult.NotFound,
            ChatProtocol.ApplyOperation(
                new Chatroom("room", "host"),
                new MessageEdited(Guid.NewGuid(), "Edited", "guest", Guid.NewGuid())));
    }

    [Fact]
    public void ApplyUnsupportedOperationReturnsForbidden()
    {
        Assert.Equal(
            RoomResult.Forbidden,
            ChatProtocol.ApplyOperation(new Chatroom("room", "host"), new UnknownOperation()));
    }

    private static ChatMessageWithThreads NewMessage(string sender, string content)
    {
        return new ChatMessageWithThreads { SenderName = sender, Content = content };
    }

    private sealed record UnknownOperation : ChatOperation;
}
