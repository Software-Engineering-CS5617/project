using System.Net;

namespace ScreenShare;

/// <summary>
/// Watches traffic for ResilienceCommunicator
/// needs to be thread-safe as sending and receiving threads can use it concurrently
/// </summary>
public interface ITrafficObserver
{
    /// <summary>a ScreenShare message arrived before ScreenShare sees it</summary>
    void OnInbound(byte[] message);

    /// <summary>a ScreenShare message is about to be sent to one endpoint</summary>
    void OnOutbound(IPEndPoint to, byte[] message);

    /// <summary>false to drop a send like to a peer already declared gone</summary>
    bool ShouldSend(IPEndPoint to);
}
