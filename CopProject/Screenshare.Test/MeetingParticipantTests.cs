using ScreenShare;
namespace Screenshare.Tests;

public class MeetingParticipantTests
{
    private readonly MeetingParticipant _hostParticipant = new MeetingParticipant("Sumit", "Sgsuper12373", true);
    private readonly MeetingParticipant _clientParticipant = new MeetingParticipant("Sumit", "clientSumit", false);

    [Fact]
    public void StoreValues()
    {
        Assert.Equal("Sumit", _hostParticipant.Id);
        Assert.Equal("Sgsuper12373", _hostParticipant.DisplayName);
        Assert.True(_hostParticipant.IsHost);

        Assert.Equal("Sumit", _clientParticipant.Id);
        Assert.Equal("clientSumit", _clientParticipant.DisplayName);
        Assert.False(_clientParticipant.IsHost);
    }

    [Fact]
    public void RejectEmptyId()
    {
        Assert.Throws<ArgumentNullException>(() => new MeetingParticipant(null!, "sgsuper12373", true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectBlankId(string id)
    {
        Assert.Throws<ArgumentException>(() => new MeetingParticipant(id, "sgsuper12373", true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectBlankDisplayName(string displayName)
    {
        Assert.Throws<ArgumentException>(() => new MeetingParticipant("Sumit", displayName, true));
    }

    [Fact]
    public void RejectEmptyDisplayName()
    {
        Assert.Throws<ArgumentNullException>(() => new MeetingParticipant("Sumit", null!, true));
    }
}
