using System;
using System.Linq;
using Chat;

namespace Chat.Test;

public class ChatMessageTests
{
    [Fact]
    public void NewMessageStartsWithDefaultContentAndIsNotDeleted()
    {
        ChatMessage message = new() { SenderName = "guest" };

        Assert.Equal(string.Empty, message.Content);
        Assert.Equal(0, message.EditCount);
        Assert.False(message.IsDeleted);
        Assert.Null(message.DeletedByUser);
        Assert.Empty(message.Reactions);
    }

    [Fact]
    public void EditReplacesContentAndIncrementsEditCount()
    {
        ChatMessage message = new() { SenderName = "guest", Content = "First version" };

        message.Edit("Second version");
        message.Edit("Third version");

        Assert.Equal("Third version", message.Content);
        Assert.Equal(2, message.EditCount);
    }

    [Fact]
    public void EditThrowsForNullContent()
    {
        ChatMessage message = new() { SenderName = "guest" };

        Assert.Throws<ArgumentNullException>(() => message.Edit(null!));
    }

    [Fact]
    public void EditThrowsAfterMessageIsDeleted()
    {
        ChatMessage message = new() { SenderName = "guest" };
        message.Delete("host");

        Assert.Throws<InvalidOperationException>(() => message.Edit("Updated"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DeleteThrowsForNullOrWhitespaceDeleter(string? deleter)
    {
        ChatMessage message = new() { SenderName = "guest" };

        Assert.ThrowsAny<ArgumentException>(() => message.Delete(deleter!));
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void DeleteRecordsDeleterAndMarksMessageDeleted()
    {
        ChatMessage message = new() { SenderName = "guest" };

        message.Delete("moderator");

        Assert.True(message.IsDeleted);
        Assert.Equal("moderator", message.DeletedByUser);
    }

    [Fact]
    public void DeleteThrowsWhenMessageWasAlreadyDeleted()
    {
        ChatMessage message = new() { SenderName = "guest" };
        message.Delete("moderator");

        Assert.Throws<InvalidOperationException>(() => message.Delete("guest"));
        Assert.Equal("moderator", message.DeletedByUser);
    }

    [Theory]
    [InlineData(null, "guest")]
    [InlineData("", "guest")]
    [InlineData(" ", "guest")]
    [InlineData("👍", null)]
    [InlineData("👍", "")]
    [InlineData("👍", " ")]
    public void ToggleReactionThrowsForNullOrWhitespaceArguments(string? emoji, string? user)
    {
        ChatMessage message = new() { SenderName = "guest" };

        Assert.ThrowsAny<ArgumentException>(() => message.ToggleReaction(emoji!, user!));
    }

    [Fact]
    public void ToggleReactionAddsReactionWhenUserHasNotReacted()
    {
        ChatMessage message = new() { SenderName = "guest" };

        Assert.True(message.ToggleReaction("👍", "guest"));
        Assert.Contains("guest", message.Reactions["👍"]);
    }

    [Fact]
    public void ToggleReactionRemovesExistingUserReactionAndKeepsOtherUsers()
    {
        ChatMessage message = new() { SenderName = "guest" };
        message.ToggleReaction("👍", "guest");
        message.ToggleReaction("👍", "host");

        Assert.False(message.ToggleReaction("👍", "guest"));
        Assert.Equal(new[] { "host" }, message.Reactions["👍"]);
    }

    [Fact]
    public void ToggleReactionRemovesEmojiEntryWhenLastUserUntoggles()
    {
        ChatMessage message = new() { SenderName = "guest" };
        message.ToggleReaction("👍", "guest");

        Assert.False(message.ToggleReaction("👍", "guest"));
        Assert.Empty(message.Reactions);
    }

    [Fact]
    public void ToggleReactionAddsUserToExistingEmojiReaction()
    {
        ChatMessage message = new() { SenderName = "guest" };
        message.ToggleReaction("👍", "guest");

        Assert.True(message.ToggleReaction("👍", "host"));
        Assert.Equal(2, message.Reactions["👍"].Count);
        Assert.Contains("guest", message.Reactions["👍"]);
        Assert.Contains("host", message.Reactions["👍"]);
    }

    [Fact]
    public void ToggleReactionThrowsAfterMessageIsDeleted()
    {
        ChatMessage message = new() { SenderName = "guest" };
        message.Delete("moderator");

        Assert.Throws<InvalidOperationException>(() => message.ToggleReaction("👍", "guest"));
    }
}
