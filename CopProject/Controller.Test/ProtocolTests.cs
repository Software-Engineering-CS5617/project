using System;
using Controller;
using Utils;
using Xunit;

namespace Controller.Test;

public class ProtocolTests
{
    [Fact]
    public void FormatJoinRequest()
    {
        var user = new ControllerUser("Bala", "bala@xyz.com", null, "10.10.10.10", 9999, isHost:true);
        string message = Protocol.FormatJoinRequest(user);
        Assert.StartsWith("JOIN|", message);
        Assert.Contains("bala@xyz.com", message);
    }

    [Fact]
    public void FormatLeaveRequest()
    {
        string message = Protocol.FormatLeaveRequest("bala@xyz.com#10.10.10.10#9999");
        Assert.Equal("LEAVE|bala@xyz.com#10.10.10.10#9999", message);

        string LeaveMsg = Protocol.FormatNotification(Protocol.NotifySessionEnded, "HostClosed");
        Assert.Equal("SESSION_ENDED|HostClosed", LeaveMsg);
    }

    [Fact]
    public void ParseValidMessage()
    {
        string raw = "JOIN|user123|extraInfo";
        var result = Protocol.Parse(raw);
        Assert.NotNull(result);
        Assert.Equal("JOIN", result.Type);
        Assert.Equal("user123", result.UserId);
        Assert.Equal("user123", result.Payload);
        Assert.Single(result.Extra);
        Assert.Equal("extraInfo", result.Extra[0]);
        var res = Protocol.Parse("JOIN|user123");
        Assert.NotNull(res);
        Assert.Equal("JOIN", res.Type);
        Assert.Equal("user123", res.UserId);
        Assert.Empty(res.Extra);

    }

    [Fact]
    public void ParseInvalidMessage()
    {
        Assert.Null(Protocol.Parse(null!));
        Assert.Null(Protocol.Parse(""));
        Assert.Null(Protocol.Parse("JUST_A_WORD_WITH_NO_SEPARATOR"));
    }

}