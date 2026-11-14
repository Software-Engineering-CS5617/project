using System.Net;
using ScreenShare;

namespace Screenshare.Tests;

public class ScreenShareMessageKindTests
{
    private static readonly Participant s_viewer = new("sumit", new IPEndPoint(IPAddress.Loopback, 5000));

    [Theory]
    [InlineData(ScreenShareMessageKind.Data, 1)]
    [InlineData(ScreenShareMessageKind.StreamStarted, 2)]
    [InlineData(ScreenShareMessageKind.StreamStopped, 3)]
    [InlineData(ScreenShareMessageKind.Subscribe, 4)]
    [InlineData(ScreenShareMessageKind.Unsubscribe, 5)]
    [InlineData(ScreenShareMessageKind.RequestFullFrame, 6)]
    [InlineData(ScreenShareMessageKind.FrameAck, 7)]
    [InlineData(ScreenShareMessageKind.UpdateSendToList, 8)]
    [InlineData(ScreenShareMessageKind.Hello, 9)]
    public void Kinds_HaveFixedWireValues(ScreenShareMessageKind kind, byte value)
    {
        Assert.Equal(value, (byte)kind);
    }

    [Fact]
    public void Kinds_AreAllListed()
    {
        // a new kind must also get a row in Kinds_HaveFixedWireValues above
        Assert.Equal(9, Enum.GetValues<ScreenShareMessageKind>().Length);
    }

    [Fact]
    public void EveryControlMessage_HasItsOwnKind_IncludingHello()
    {
        IScreenShareMessage[] messages =
        {
            new StreamStarted("aryan", s_viewer, "Aryan", 1280, 720),
            new StreamStopped("aryan"),
            new Subscribe(s_viewer, "aryan", 720),
            new Unsubscribe(s_viewer, "aryan"),
            new RequestFullFrame(s_viewer, "aryan"),
            new FrameAck(s_viewer, "aryan", 0),
            new UpdateSendToList("aryan", Array.Empty<IPEndPoint>()),
            new Hello(s_viewer),
        };

        Assert.Equal(8, messages.Length);
        Assert.Equal(messages.Length, messages.Select(m => m.Kind).Distinct().Count());
        Assert.DoesNotContain(messages, m => m.Kind == ScreenShareMessageKind.Data);
    }
}
