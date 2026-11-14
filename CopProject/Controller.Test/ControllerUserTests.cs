using System;
using Utils;
using Xunit;

namespace Controller.Test;

public class ControllerUserTests
{
    [Fact]
    public void DefaultConstructor()
    {
        var user = new ControllerUser();
        Assert.Equal(string.Empty, user.Username);
        Assert.Equal(string.Empty, user.Gmail);
        Assert.Equal(string.Empty, user.Ip);
        Assert.Equal(0,user.Port);
        Assert.Null(user.ProfilePicture);
        Assert.False(user.IsHost);

    }

    [Fact]
    public void CreateHostUser()
    {
        var user = new ControllerUser("Bala", "bala@xyz.com", null, "10.10.10.10", 9999, isHost:true);
        Assert.Equal("Bala", user.Username);
        Assert.Equal("bala@xyz.com", user.Gmail);
        Assert.Equal("10.10.10.10", user.Ip);
        Assert.Equal(9999, user.Port);
        Assert.Null(user.ProfilePicture);
        Assert.True(user.IsHost);
        Assert.Equal("Host",user.RoleDisplay);
        Assert.Equal("bala@xyz.com", user.UserKey);
    }

    [Fact]
    public void CreateClientUser()
    {
        var user = new ControllerUser("Bala", "bala@xyz.com", null, "10.10.10.10", 9999, isHost:false);
        Assert.Equal("Bala", user.Username);
        Assert.Equal("bala@xyz.com", user.Gmail);
        Assert.Equal("10.10.10.10", user.Ip);
        Assert.Equal(9999, user.Port);
        Assert.Null(user.ProfilePicture);
        Assert.False(user.IsHost);
        Assert.Equal("Client",user.RoleDisplay);
    }

}
