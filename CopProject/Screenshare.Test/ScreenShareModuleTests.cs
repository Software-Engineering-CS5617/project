using System;
using Xunit;
using ScreenShare;

namespace Screenshare.Tests;

public class ScreenShareModuleTests
{
    [Fact]
    public void PriorityFor_DataAndControlMessages_ReturnsExpectedPriorities()
    {
        ScreenDataPacket dataMessage = new ScreenDataPacket(
            "sanjays-stream", 1, DateTime.UtcNow, 64, 64, DataPacketType.Pulse, tileSize: 32);
        StreamStopped controlMessage = new StreamStopped("sanjays-stream");

        int dataPriority = ScreenShareModule.PriorityFor(dataMessage);
        int controlPriority = ScreenShareModule.PriorityFor(controlMessage);

        Assert.Equal(8, ScreenShareModule.ModuleId);
        Assert.Equal(1, ScreenShareModule.DataPriority);
        Assert.Equal(3, ScreenShareModule.ControlPriority);
        Assert.Equal(ScreenShareModule.DataPriority, dataPriority);
        Assert.Equal(ScreenShareModule.ControlPriority, controlPriority);
    }
}
