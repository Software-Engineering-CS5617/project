using System;
using System.Net;
using Xunit;

using ScreenShare;

namespace Screenshare.Tests;

public class ScreenShareMessageListenerTests
{
    [Fact]
    public void Constructor_NullOnMessage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenShareMessageListener(null!));
    }

    [Fact]
    public void OnMessageReceived_ValidControlMessage_InvokesCallbackWithDecodedAndRawBytes()
    {
        IScreenShareMessage? receivedMessage = null;
        byte[]? receivedRawBytes = null;
        int callCount = 0;

        ScreenShareMessageListener listener = new ScreenShareMessageListener((decoded, raw) => {
            callCount++;
            receivedMessage = decoded;
            receivedRawBytes = raw;
        });

        StreamStopped expectedMessage = new StreamStopped("stream-123");
        byte[] payload = ScreenShareCodec.Encode(expectedMessage);

        listener.OnMessageReceived(payload);

        Assert.Equal(1, callCount);
        StreamStopped actualStopped = Assert.IsType<StreamStopped>(receivedMessage);
        Assert.Equal("stream-123", actualStopped.StreamId);
        Assert.Same(payload, receivedRawBytes);
    }

    [Fact]
    public void OnMessageReceived_ValidParticipantMessage_DecodesAndInvokesCallback()
    {
        IScreenShareMessage? receivedMessage = null;
        ScreenShareMessageListener listener = new ScreenShareMessageListener((decoded, _) => receivedMessage = decoded);

        Participant viewer = new Participant("viewer-1", IPEndPoint.Parse("127.0.0.1:5000"));
        byte[] payload = ScreenShareCodec.Encode(new Hello(viewer));

        listener.OnMessageReceived(payload);

        Hello actualHello = Assert.IsType<Hello>(receivedMessage);
        Assert.Equal("viewer-1", actualHello.Viewer.Id);
        Assert.Equal(IPAddress.Loopback, actualHello.Viewer.Endpoint.Address);
        Assert.Equal(5000, actualHello.Viewer.Endpoint.Port);
    }


    [Fact]
    public void OnMessageReceived_TrailingBytes_DoesNotInvokeCallback()
    {
        bool callbackInvoked = false;
        ScreenShareMessageListener listener = new ScreenShareMessageListener((_, _) => callbackInvoked = true);

        byte[] valid = ScreenShareCodec.Encode(new StreamStopped("stream-123"));
        byte[] withTrailingByte = [.. valid, 0x00];

        listener.OnMessageReceived(withTrailingByte);

        Assert.False(callbackInvoked);
    }

    [Fact]
    public void OnMessageReceived_NullPayload_DoesNotThrowOrInvokeCallback()
    {
        bool callbackInvoked = false;
        ScreenShareMessageListener listener = new ScreenShareMessageListener((_, _) => callbackInvoked = true);

        listener.OnMessageReceived(null!);

        Assert.False(callbackInvoked);
    }
}
