using System;
using Xunit;
using ScreenShare;

namespace Screenshare.Tests;

public class ScreenShareOptionsTests
{
    [Fact]
    public void DefaultOptions_HaveExpectedValues()
    {
        var options = new ScreenShareOptions();

        Assert.Equal(50, options.FramesPerSecond);
        Assert.Equal(480, options.MaxHeight);
        Assert.Equal(8, options.MaxFramesInFlight);
        Assert.Equal(TimeSpan.FromSeconds(2), options.AckTimeout);
        Assert.Equal(5, options.MaxViewerLag);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ViewerTimeout);
        Assert.Equal(TimeSpan.FromSeconds(1), options.MaintenanceInterval);
    }

    [Fact]
    public void Validate_WithDefaultOptions_DoesNotThrowAnError()
    {
        var options = new ScreenShareOptions();

        // Record.Exception captures any errors thrown by the Validate method
        Exception exception = Record.Exception(options.Validate);

        // If the exception is null, it means no errors were thrown (which is what we want!)
        Assert.Null(exception);
    }


    // ---------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ThrowsError_WhenFramesPerSecondIsInvalid(int invalidValue)
    {
        //  Create default options, but change just this one property
        ScreenShareOptions options = new ScreenShareOptions() with { FramesPerSecond = invalidValue };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ThrowsError_WhenMaxHeightIsInvalid(int invalidValue)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { MaxHeight = invalidValue };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ThrowsError_WhenMaxFramesInFlightIsInvalid(int invalidValue)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { MaxFramesInFlight = invalidValue };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ThrowsError_WhenMaxViewerLagIsInvalid(int invalidValue)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { MaxViewerLag = invalidValue };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ThrowsError_WhenAckTimeoutIsInvalid(int seconds)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { AckTimeout = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ThrowsError_WhenViewerTimeoutIsInvalid(int seconds)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { ViewerTimeout = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ThrowsError_WhenMaintenanceIntervalIsInvalid(int seconds)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { MaintenanceInterval = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }
}
