using System;
using System.Collections.Generic;
using System.Linq;
using Chat;

namespace Chat.Test;

public class ChatroomTests
{
    [Theory]
    [InlineData(null, "host")]
    [InlineData("", "host")]
    [InlineData(" ", "host")]
    [InlineData("room", null)]
    [InlineData("room", "")]
    [InlineData("room", " ")]
    public void ConstructorRejectsNullOrWhitespaceNames(string? roomName, string? hostName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Chatroom(roomName!, hostName!));
    }

    [Fact]
    public void ConstructorSetsRoomAndHostAndAddsHostAsParticipant()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Equal("room", chatroom.RoomName);
        Assert.Equal("host", chatroom.HostName);
        Assert.Contains("host", chatroom.Participants);
    }

    [Fact]
    public void AddParticipantAddsNameAndReturnsFalseForDuplicate()
    {
        Chatroom chatroom = new("room", "host");

        Assert.True(chatroom.AddParticipant("guest"));
        Assert.False(chatroom.AddParticipant("guest"));
        Assert.Contains("guest", chatroom.Participants);
    }

    [Fact]
    public void ParticipantsReturnsSnapshot()
    {
        Chatroom chatroom = new("room", "host");
        IReadOnlySet<string> snapshot = chatroom.Participants;

        chatroom.AddParticipant("guest");

        Assert.DoesNotContain("guest", snapshot);
        Assert.Contains("guest", chatroom.Participants);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddParticipantRejectsNullOrWhitespaceName(string? name)
    {
        Chatroom chatroom = new("room", "host");

        Assert.ThrowsAny<ArgumentException>(() => chatroom.AddParticipant(name!));
    }

    [Fact]
    public void RemoveParticipantRemovesNameAndReturnsFalseWhenMissing()
    {
        Chatroom chatroom = new("room", "host");
        chatroom.AddParticipant("guest");

        Assert.True(chatroom.RemoveParticipant("guest"));
        Assert.False(chatroom.RemoveParticipant("guest"));
        Assert.DoesNotContain("guest", chatroom.Participants);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RemoveParticipantRejectsNullOrWhitespaceName(string? name)
    {
        Chatroom chatroom = new("room", "host");

        Assert.ThrowsAny<ArgumentException>(() => chatroom.RemoveParticipant(name!));
    }

    [Fact]
    public void AddMessageRejectsNull()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Throws<ArgumentNullException>(() => chatroom.AddMessage(null!));
    }

    [Fact]
    public void AddMessageStoresMessageByItsAssignedId()
    {
        Chatroom chatroom = new("room", "host");
        ChatMessageWithThreads message = NewMessage("guest", "Hello");

        Guid id = chatroom.AddMessage(message);

        Assert.True(chatroom.TryGetMessage(id, out ChatMessage found));
        Assert.Same(message, found);
    }

    [Fact]
    public void TryGetMessageFindsRoomMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Hello"));

        Assert.True(chatroom.TryGetMessage(id, out ChatMessage found));
        Assert.Equal("Hello", found.Content);
    }

    [Fact]
    public void TryGetMessageFindsThreadReply()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));
        Guid replyId = chatroom.AddThreadMessage(parentId, new ChatMessage { SenderName = "host" });

        Assert.True(chatroom.TryGetMessage(replyId, out ChatMessage reply));
        Assert.Equal("host", reply.SenderName);
    }

    [Fact]
    public void TryGetMessageContinuesSearchingAfterReplyIsAbsentFromEarlierThread()
    {
        Chatroom chatroom = new("room", "host");
        chatroom.AddMessage(NewMessage("guest", "First root"));
        Guid secondParentId = chatroom.AddMessage(NewMessage("moderator", "Second root"));
        Guid replyId = chatroom.AddThreadMessage(
            secondParentId,
            new ChatMessage { SenderName = "host", Content = "Reply" });

        Assert.True(chatroom.TryGetMessage(replyId, out ChatMessage reply));
        Assert.Equal("Reply", reply.Content);
    }

    [Fact]
    public void TryGetMessageReturnsFalseForMissingId()
    {
        Chatroom chatroom = new("room", "host");

        Assert.False(chatroom.TryGetMessage(Guid.NewGuid(), out _));
    }

    [Fact]
    public void EditMessageRejectsInvalidArguments()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Original"));

        Assert.Throws<ArgumentNullException>(() => chatroom.EditMessage(id, null!, "guest"));
        Assert.ThrowsAny<ArgumentException>(() => chatroom.EditMessage(id, "Updated", " "));
    }

    [Fact]
    public void EditMessageReturnsNotFoundForMissingMessage()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Equal(RoomResult.NotFound, chatroom.EditMessage(Guid.NewGuid(), "Updated", "guest"));
    }

    [Fact]
    public void EditMessageForbidsEditingAnotherUsersMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Original"));

        Assert.Equal(RoomResult.Forbidden, chatroom.EditMessage(id, "Updated", "host"));
    }

    [Fact]
    public void EditMessageUpdatesOwnMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Original"));

        Assert.Equal(RoomResult.Ok, chatroom.EditMessage(id, "Updated", "guest"));
        Assert.True(chatroom.TryGetMessage(id, out ChatMessage edited));
        Assert.Equal("Updated", edited.Content);
    }

    [Fact]
    public void EditMessageForbidsEditingDeletedMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Original"));
        Assert.Equal(RoomResult.Ok, chatroom.DeleteMessage(id, "guest"));

        Assert.Equal(RoomResult.Forbidden, chatroom.EditMessage(id, "Updated", "guest"));
    }

    [Fact]
    public void DeleteMessageRejectsInvalidDeleter()
    {
        Chatroom chatroom = new("room", "host");

        Assert.ThrowsAny<ArgumentException>(() => chatroom.DeleteMessage(Guid.NewGuid(), ""));
    }

    [Fact]
    public void DeleteMessageReturnsNotFoundForMissingMessage()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Equal(RoomResult.NotFound, chatroom.DeleteMessage(Guid.NewGuid(), "guest"));
    }

    [Fact]
    public void DeleteMessageAllowsHostToDeleteAnotherUsersMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Hello"));

        Assert.Equal(RoomResult.Ok, chatroom.DeleteMessage(id, "host"));
    }

    [Fact]
    public void DeleteMessageMarksOwnMessageDeleted()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Hello"));

        Assert.Equal(RoomResult.Ok, chatroom.DeleteMessage(id, "guest"));
        Assert.True(chatroom.TryGetMessage(id, out ChatMessage deleted));
        Assert.True(deleted.IsDeleted);
        Assert.Equal("guest", deleted.DeletedByUser);
    }

    [Fact]
    public void DeleteMessageForbidsDeletingAlreadyDeletedMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Hello"));
        Assert.Equal(RoomResult.Ok, chatroom.DeleteMessage(id, "guest"));

        Assert.Equal(RoomResult.Forbidden, chatroom.DeleteMessage(id, "guest"));
    }

    [Fact]
    public void ToggleReactionRejectsInvalidArguments()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Hello"));

        Assert.ThrowsAny<ArgumentException>(() => chatroom.ToggleReaction(id, " ", "guest"));
        Assert.ThrowsAny<ArgumentException>(() => chatroom.ToggleReaction(id, "👍", ""));
    }

    [Fact]
    public void ToggleReactionReturnsNotFoundForMissingMessage()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Equal(RoomResult.NotFound, chatroom.ToggleReaction(Guid.NewGuid(), "👍", "guest"));
    }

    [Fact]
    public void ToggleReactionAddsReactionToMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Hello"));

        Assert.Equal(RoomResult.Ok, chatroom.ToggleReaction(id, "👍", "guest"));
        Assert.True(chatroom.TryGetMessage(id, out ChatMessage message));
        Assert.Contains("guest", message.Reactions["👍"]);
    }

    [Fact]
    public void ToggleReactionForbidsReactionOnDeletedMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Hello"));
        Assert.Equal(RoomResult.Ok, chatroom.DeleteMessage(id, "guest"));

        Assert.Equal(RoomResult.Forbidden, chatroom.ToggleReaction(id, "👍", "guest"));
    }

    [Fact]
    public void EditMessageReturnsNotFoundWhenThreadParentIsMissing()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Equal(
            RoomResult.NotFound,
            chatroom.EditMessage(Guid.NewGuid(), "Edited", "host", Guid.NewGuid()));
    }

    [Fact]
    public void EditMessageReturnsNotFoundWhenReplyIsNotInSpecifiedThread()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));

        Assert.Equal(RoomResult.NotFound, chatroom.EditMessage(Guid.NewGuid(), "Edited", "guest", parentId));
    }

    [Fact]
    public void EditMessageCanEditReplyInSpecifiedThread()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));
        Guid replyId = chatroom.AddThreadMessage(parentId, new ChatMessage { SenderName = "host" });

        Assert.Equal(RoomResult.Ok, chatroom.EditMessage(replyId, "Edited", "host", parentId));
    }

    [Fact]
    public void DeleteMessageCanDeleteReplyInSpecifiedThread()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));
        Guid replyId = chatroom.AddThreadMessage(parentId, new ChatMessage { SenderName = "host" });

        Assert.Equal(RoomResult.Ok, chatroom.DeleteMessage(replyId, "host", parentId));
    }

    [Fact]
    public void ToggleReactionCanReactToReplyInSpecifiedThread()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("guest", "Root"));
        Guid replyId = chatroom.AddThreadMessage(parentId, new ChatMessage { SenderName = "host" });

        Assert.Equal(RoomResult.Ok, chatroom.ToggleReaction(replyId, "👍", "guest", parentId));
    }

    [Fact]
    public void AddThreadMessageRejectsNullReply()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Throws<ArgumentNullException>(() => chatroom.AddThreadMessage(Guid.NewGuid(), null!));
    }

    [Fact]
    public void AddThreadMessageThrowsWhenParentIsMissing()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Throws<KeyNotFoundException>(
            () => chatroom.AddThreadMessage(Guid.NewGuid(), new ChatMessage { SenderName = "guest" }));
    }

    [Fact]
    public void AddMessageWithExistingIdRejectsNullMessage()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Throws<ArgumentNullException>(() => chatroom.AddMessage(Guid.NewGuid(), null!));
    }

    [Fact]
    public void AddMessageWithExistingIdRejectsDuplicateId()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = Guid.NewGuid();
        chatroom.AddMessage(id, NewMessage("guest", "First"));

        Assert.Throws<ArgumentException>(() => chatroom.AddMessage(id, NewMessage("guest", "Duplicate")));
    }

    [Fact]
    public void SetParticipantsRejectsNull()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Throws<ArgumentNullException>(() => chatroom.SetParticipants(null!));
    }

    [Fact]
    public void SetParticipantsReplacesParticipantSet()
    {
        Chatroom chatroom = new("room", "host");
        chatroom.AddParticipant("old");

        chatroom.SetParticipants(new[] { "guest", "moderator" });

        Assert.Equal(new[] { "guest", "moderator" }, chatroom.Participants.OrderBy(name => name));
    }

    [Fact]
    public void ClearMessagesRemovesMessagesButKeepsParticipants()
    {
        Chatroom chatroom = new("room", "host");
        Guid id = chatroom.AddMessage(NewMessage("guest", "Hello"));

        chatroom.ClearMessages();

        Assert.False(chatroom.TryGetMessage(id, out _));
        Assert.Contains("host", chatroom.Participants);
    }

    [Fact]
    public void AddThreadMessageByIdReturnsNotFoundWhenParentIsMissing()
    {
        Chatroom chatroom = new("room", "host");

        Assert.Equal(
            RoomResult.NotFound,
            chatroom.AddThreadMessageById(Guid.NewGuid(), Guid.NewGuid(), new ChatMessage { SenderName = "guest" }));
    }

    [Fact]
    public void AddThreadMessageByIdAddsReplyUsingSuppliedId()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("host", "Root"));
        Guid replyId = Guid.NewGuid();

        Assert.Equal(
            RoomResult.Ok,
            chatroom.AddThreadMessageById(parentId, replyId, new ChatMessage { SenderName = "guest" }));
        Assert.True(chatroom.TryGetMessage(replyId, out ChatMessage reply));
        Assert.Equal("guest", reply.SenderName);
    }

    [Fact]
    public void AddThreadMessageByIdRejectsDuplicateReplyId()
    {
        Chatroom chatroom = new("room", "host");
        Guid parentId = chatroom.AddMessage(NewMessage("host", "Root"));
        Guid replyId = Guid.NewGuid();
        chatroom.AddThreadMessageById(parentId, replyId, new ChatMessage { SenderName = "guest" });

        Assert.Throws<ArgumentException>(
            () => chatroom.AddThreadMessageById(parentId, replyId, new ChatMessage { SenderName = "other" }));
    }

    private static ChatMessageWithThreads NewMessage(string sender, string content)
    {
        return new ChatMessageWithThreads { SenderName = sender, Content = content };
    }
}
