using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
            () => AddMessageToThreadWithId(message, id, null!));

        Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Empty(message.ThreadById);
    }

    [Fact]
    public void AddingDuplicateInternalThreadIdThrows()
    {
        ChatMessageWithThreads message = new() { SenderName = "guest" };
        Guid replyId = Guid.NewGuid();
        ChatMessage reply = new() { SenderName = "host" };
        AddMessageToThreadWithId(message, replyId, reply);

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
            () => AddMessageToThreadWithId(message, replyId, reply));

        Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Single(message.ThreadById);
    }

    private static void AddMessageToThreadWithId(
        ChatMessageWithThreads message,
        Guid id,
        ChatMessage reply)
    {
        MethodInfo method = typeof(ChatMessageWithThreads).GetMethod(
            "AddMessageToThread",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(Guid), typeof(ChatMessage) },
            modifiers: null)!;
        method.Invoke(message, new object[] { id, reply });
    }
}
