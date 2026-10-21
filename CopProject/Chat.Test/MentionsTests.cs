using System;
using Chat;

namespace Chat.Test;

public class MentionsTests
{
    [Fact]
    public void IsUserMentionedReturnsFalseWhenUserIsNotMentioned()
    {
        Mentions mentions = new();

        Assert.False(mentions.IsUserMentioned("guest"));
    }

    [Fact]
    public void IsUserMentionedReturnsTrueForExplicitlyMentionedUser()
    {
        Mentions mentions = new();
        mentions.Usernames.Add("guest");

        Assert.True(mentions.IsUserMentioned("guest"));
        Assert.False(mentions.IsUserMentioned("host"));
    }

    [Fact]
    public void IsUserMentionedUsesCaseSensitiveUsernameMatching()
    {
        Mentions mentions = new();
        mentions.Usernames.Add("Guest");

        Assert.True(mentions.IsUserMentioned("Guest"));
        Assert.False(mentions.IsUserMentioned("guest"));
    }

    [Fact]
    public void AllMentionsEveryUsername()
    {
        Mentions mentions = new() { All = true };

        Assert.True(mentions.IsUserMentioned("guest"));
        Assert.True(mentions.IsUserMentioned("host"));
    }

    [Fact]
    public void IsUserMentionedThrowsForNullUsername()
    {
        Mentions mentions = new();

        Assert.Throws<ArgumentNullException>(() => mentions.IsUserMentioned(null!));
    }
}