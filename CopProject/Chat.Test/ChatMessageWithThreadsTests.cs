using System;
using System.Collections.Generic;
using System.Linq;
using Chat;

namespace Chat.Test;

public class ChatMessageWithThreadsTests
{
    [Fact]
    public void NewThreadedMessageStartsWithEmptyThreadIndexes()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };

        Assert.Empty(message.ThreadById);
        Assert.Empty(message.ThreadByTime);
    }

    [Fact]
    public void AddMessageToThreadThrowsForNullReply()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };

        Assert.Throws<ArgumentNullException>(() => message.AddMessageToThread(null!));
        Assert.Empty(message.ThreadById);
        Assert.Empty(message.ThreadByTime);
    }

    [Fact]
    public void AddMessageToThreadIndexesReplyByIdAndTime()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };
        ChatMessage reply = new()
        {
            SenderName = "host",
            Timestamp = DateTimeOffset.Parse("2026-01-01T12:00:00+00:00"),
            Content = "Reply"
        };

        message.AddMessageToThread(reply);

        KeyValuePair<Guid, ChatMessage> byId = Assert.Single(message.ThreadById);
        KeyValuePair<(DateTimeOffset Timestamp, Guid Id), ChatMessage> byTime = Assert.Single(message.ThreadByTime);
        Assert.Same(reply, byId.Value);
        Assert.Equal(byId.Key, byTime.Key.Id);
        Assert.Equal(reply.Timestamp, byTime.Key.Timestamp);
        Assert.Same(reply, byTime.Value);
    }

    [Fact]
    public void ThreadMessagesAreOrderedByTimestampThenId()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };
        ChatMessage laterReply = new()
        {
            SenderName = "host",
            Timestamp = DateTimeOffset.Parse("2026-01-01T12:02:00+00:00"),
            Content = "Later"
        };
        ChatMessage earlierReply = new()
        {
            SenderName = "host",
            Timestamp = DateTimeOffset.Parse("2026-01-01T12:01:00+00:00"),
            Content = "Earlier"
        };
        message.AddMessageToThread(laterReply);
        message.AddMessageToThread(earlierReply);

        Assert.Equal(
            new[] { "Earlier", "Later" },
            message.ThreadByTime.Values.Select(reply => reply.Content));
    }

    [Fact]
    public void AddingNullReplyThroughInternalIdOverloadThrows()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };
        Guid id = Guid.NewGuid();

        Assert.Throws<ArgumentNullException>(() => message.AddMessageToThread(id, null!));
        Assert.Empty(message.ThreadById);
    }

    [Fact]
    public void AddingDuplicateInternalThreadIdThrows()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };
        Guid replyId = Guid.NewGuid();
        ChatMessage reply = new() { SenderName = "host" };
        message.AddMessageToThread(replyId, reply);

        Assert.Throws<ArgumentException>(() => message.AddMessageToThread(replyId, reply));
        Assert.Single(message.ThreadById);
    }

    [Fact]
    public void TryGetThreadMessageReturnsReplyWhenPresent()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };
        Guid replyId = Guid.NewGuid();
        ChatMessage reply = new() { SenderName = "host", Content = "Reply" };
        message.AddMessageToThread(replyId, reply);

        bool found = message.TryGetThreadMessage(replyId, out ChatMessage result);

        Assert.True(found);
        Assert.Same(reply, result);
    }

    [Fact]
    public void TryGetThreadMessageReturnsFalseWhenReplyIsMissing()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };

        bool found = message.TryGetThreadMessage(Guid.NewGuid(), out ChatMessage? result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void DeepCloneCopiesRepliesAndTheirMutableState()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest", Content = "Root" };
        Guid replyId = Guid.NewGuid();
        ChatMessage reply = new() { SenderName = "host", Content = "Reply" };
        reply.Edit("Edited reply");
        reply.ToggleReaction("👍", "guest");
        message.AddMessageToThread(replyId, reply);

        ChatMessageWithThreads clone = message.DeepClone();
        clone.ThreadById[replyId].Reactions["👍"].Add("clone-only");

        Assert.NotSame(message, clone);
        Assert.Equal("Root", clone.Content);
        Assert.Equal("Edited reply", clone.ThreadById[replyId].Content);
        Assert.Equal(1, clone.ThreadById[replyId].EditCount);
        Assert.Contains("guest", clone.ThreadById[replyId].Reactions["👍"]);
        Assert.DoesNotContain("clone-only", reply.Reactions["👍"]);
        Assert.Single(clone.ThreadByTime);
    }

    [Fact]
    public void DeepCloneOfMessageWithoutRepliesHasNoThreadReplies()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest", Content = "Root" };

        ChatMessageWithThreads clone = message.DeepClone();

        Assert.NotSame(message, clone);
        Assert.Equal(message.Content, clone.Content);
        Assert.Empty(clone.ThreadById);
        Assert.Empty(clone.ThreadByTime);
    }

    [Fact]
    public void DeepCloneWithoutThreadMessagesLeavesRepliesBehind()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest", Content = "Root" };
        message.AddMessageToThread(new ChatMessage { SenderName = "host", Content = "Reply" });

        ChatMessageWithThreads clone = message.DeepCloneWithoutThreadMessages();

        Assert.Equal(message.Content, clone.Content);
        Assert.Empty(clone.ThreadById);
        Assert.Empty(clone.ThreadByTime);
    }

}
