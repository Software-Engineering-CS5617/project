using System.Net;
using ScreenShare;
using Xunit;

namespace Screenshare.Tests;

/// <summary>
/// tests that control messages store their values, report their kind and reject bad arguments
/// </summary>
public class ControlMessageTests
{
    private static readonly IPEndPoint s_endpoint = new(IPAddress.Loopback, 5000);
    private static readonly Participant s_viewer = new("sumit", s_endpoint);

    // ---------- Participant ----------

    [Fact]
    public void ParticipantStoresValues()
    {
        Participant participant = new("aryan", s_endpoint);

        Assert.Equal("aryan", participant.Id);
        Assert.Equal(s_endpoint, participant.Endpoint);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParticipantRejectsEmptyId(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Participant(id!, s_endpoint));
    }

    [Fact]
    public void ParticipantRejectsNullEndpoint()
    {
        Assert.Throws<ArgumentNullException>(() => new Participant("aryan", null!));
    }

    // ---------- StreamStarted ----------

    [Fact]
    public void StreamStartedStoresValuesAndKind()
    {
        StreamStarted started = new("aryan", s_viewer, "Aryan", 1280, 720);

        Assert.Equal("aryan", started.StreamId);
        Assert.Equal(s_viewer, started.Sharer);
        Assert.Equal("Aryan", started.DisplayName);
        Assert.Equal(1280, started.Width);
        Assert.Equal(720, started.Height);
        Assert.Equal(ScreenShareMessageKind.StreamStarted, started.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StreamStartedRejectsEmptyStreamId(string? streamId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new StreamStarted(streamId!, s_viewer, "Aryan", 1280, 720));
    }

    [Fact]
    public void StreamStartedRejectsNullSharer()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamStarted("aryan", null!, "Aryan", 1280, 720));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StreamStartedRejectsEmptyDisplayName(string? displayName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new StreamStarted("aryan", s_viewer, displayName!, 1280, 720));
    }

    [Theory]
    [InlineData(0, 720)]
    [InlineData(-1, 720)]
    [InlineData(1280, 0)]
    [InlineData(1280, -1)]
    public void StreamStartedRejectsInvalidSize(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StreamStarted("aryan", s_viewer, "Aryan", width, height));
    }

    // ---------- StreamStopped ----------

    [Fact]
    public void StreamStoppedStoresValuesAndKind()
    {
        StreamStopped stopped = new("aryan");

        Assert.Equal("aryan", stopped.StreamId);
        Assert.Equal(ScreenShareMessageKind.StreamStopped, stopped.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StreamStoppedRejectsEmptyStreamId(string? streamId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new StreamStopped(streamId!));
    }

    // ---------- Subscribe ----------

    [Fact]
    public void SubscribeStoresValuesAndKind()
    {
        Subscribe subscribe = new(s_viewer, "aryan", 720);

        Assert.Equal(s_viewer, subscribe.Viewer);
        Assert.Equal("aryan", subscribe.StreamId);
        Assert.Equal(720, subscribe.MaxHeight);
        Assert.Equal(ScreenShareMessageKind.Subscribe, subscribe.Kind);
    }

    [Fact]
    public void SubscribeRejectsNullViewer()
    {
        Assert.Throws<ArgumentNullException>(() => new Subscribe(null!, "aryan", 720));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SubscribeRejectsEmptyStreamId(string? streamId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Subscribe(s_viewer, streamId!, 720));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SubscribeRejectsInvalidMaxHeight(int maxHeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Subscribe(s_viewer, "aryan", maxHeight));
    }

    // ---------- Unsubscribe ----------

    [Fact]
    public void UnsubscribeStoresValuesAndKind()
    {
        Unsubscribe unsubscribe = new(s_viewer, "aryan");

        Assert.Equal(s_viewer, unsubscribe.Viewer);
        Assert.Equal("aryan", unsubscribe.StreamId);
        Assert.Equal(ScreenShareMessageKind.Unsubscribe, unsubscribe.Kind);
    }

    [Fact]
    public void UnsubscribeRejectsNullViewer()
    {
        Assert.Throws<ArgumentNullException>(() => new Unsubscribe(null!, "aryan"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UnsubscribeRejectsEmptyStreamId(string? streamId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Unsubscribe(s_viewer, streamId!));
    }

    // ---------- RequestFullFrame ----------

    [Fact]
    public void RequestFullFrameStoresValuesAndKind()
    {
        RequestFullFrame request = new(s_viewer, "aryan");

        Assert.Equal(s_viewer, request.Viewer);
        Assert.Equal("aryan", request.StreamId);
        Assert.Equal(ScreenShareMessageKind.RequestFullFrame, request.Kind);
    }

    [Fact]
    public void RequestFullFrameRejectsNullViewer()
    {
        Assert.Throws<ArgumentNullException>(() => new RequestFullFrame(null!, "aryan"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequestFullFrameRejectsEmptyStreamId(string? streamId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new RequestFullFrame(s_viewer, streamId!));
    }

    // ---------- FrameAck ----------

    [Fact]
    public void FrameAckStoresValuesAndKind()
    {
        FrameAck ack = new(s_viewer, "aryan", 7);

        Assert.Equal(s_viewer, ack.Viewer);
        Assert.Equal("aryan", ack.StreamId);
        Assert.Equal(7, ack.FrameNumber);
        Assert.Equal(ScreenShareMessageKind.FrameAck, ack.Kind);
    }

    [Fact]
    public void FrameAckAcceptsFrameZero()
    {
        Assert.Equal(0, new FrameAck(s_viewer, "aryan", 0).FrameNumber);
    }

    [Fact]
    public void FrameAckRejectsNullViewer()
    {
        Assert.Throws<ArgumentNullException>(() => new FrameAck(null!, "aryan", 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FrameAckRejectsEmptyStreamId(string? streamId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FrameAck(s_viewer, streamId!, 1));
    }

    [Fact]
    public void FrameAckRejectsNegativeFrameNumber()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameAck(s_viewer, "aryan", -1));
    }

    // ---------- UpdateSendToList ----------

    [Fact]
    public void UpdateSendToListStoresValuesAndKind()
    {
        IPEndPoint other = new(IPAddress.Loopback, 5001);
        UpdateSendToList update = new("aryan", new[] { s_endpoint, other });

        Assert.Equal("aryan", update.StreamId);
        Assert.Equal(new[] { s_endpoint, other }, update.SendTo);
        Assert.Equal(ScreenShareMessageKind.UpdateSendToList, update.Kind);
    }

    [Fact]
    public void UpdateSendToListAcceptsEmptyList()
    {
        UpdateSendToList update = new("aryan", Array.Empty<IPEndPoint>());

        Assert.Empty(update.SendTo);
    }

    [Fact]
    public void UpdateSendToListKeepsItsOwnCopy()
    {
        List<IPEndPoint> endpoints = new() { s_endpoint };
        UpdateSendToList update = new("aryan", endpoints);

        endpoints.Add(new IPEndPoint(IPAddress.Loopback, 5001));

        Assert.Single(update.SendTo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateSendToListRejectsEmptyStreamId(string? streamId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new UpdateSendToList(streamId!, new[] { s_endpoint }));
    }

    [Fact]
    public void UpdateSendToListRejectsNullList()
    {
        Assert.Throws<ArgumentNullException>(() => new UpdateSendToList("aryan", null!));
    }

    [Fact]
    public void UpdateSendToListRejectsNullEndpoint()
    {
        Assert.Throws<ArgumentException>(() => new UpdateSendToList("aryan", new IPEndPoint[] { s_endpoint, null! }));
    }

    [Fact]
    public void UpdateSendToListRejectsNullEntryInList()
    {
        List<IPEndPoint> listOfIP = new() { s_endpoint, null! };

        Assert.Throws<ArgumentException>(() => new UpdateSendToList("myStream", listOfIP));
    }
    // ---------- all messages ----------

    [Fact]
    public void EveryControlMessageHasItsOwnKind()
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
        };

        Assert.Equal(messages.Length, messages.Select(m => m.Kind).Distinct().Count());
        Assert.DoesNotContain(messages, m => m.Kind == ScreenShareMessageKind.Data);
    }
}
