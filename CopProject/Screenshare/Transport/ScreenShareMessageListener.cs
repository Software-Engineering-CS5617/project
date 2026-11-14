using System.Diagnostics;
using Networking;

namespace ScreenShare;

internal sealed class ScreenShareMessageListener : IMessageListener
{
    private readonly Action<IScreenShareMessage, byte[]> _onMessage;
    public ScreenShareMessageListener(Action<IScreenShareMessage, byte[]> onMessage)
    {
        ArgumentNullException.ThrowIfNull(onMessage);

        _onMessage = onMessage;
    }

    public void OnMessageReceived(byte[] message)
    {
        if (ScreenShareCodec.Decode(message, out IScreenShareMessage? decoded))
        {
            _onMessage(decoded, message);
        }
        else
        {
            Trace.WriteLine($"[ScreenShare] dropped a bad message of {message?.Length ?? 0} bytes");
        }
    }
}
