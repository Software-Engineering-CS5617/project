using System.Net;
using Screenshare;
using ScreenShare;

namespace Screenshare.Tests.Fakes;

/// <summary>
/// stands in for the network when testing ScreenShareHost on its own: it only records
/// what the host asked to send, so a test can check it
/// </summary>
internal sealed class FakeRelayOutput : IRealyOutput
{
    /// <summary>every Send call, in order</summary>
    public List<(IPEndPoint To, IScreenShareMessage Message)> Sent { get; } = new();

    /// <summary>every Broadcast call, in order</summary>
    public List<IScreenShareMessage> Broadcasts { get; } = new();

    public void Send(IPEndPoint to, IScreenShareMessage message, byte[] bytes)
    {
        Sent.Add((to, message));
    }

    public void Broadcast(IScreenShareMessage message, byte[] bytes)
    {
        Broadcasts.Add(message);
    }

    /// <summary>the messages of type T sent to one endpoint</summary>
    public List<T> SentTo<T>(IPEndPoint to) where T : IScreenShareMessage
    {
        return Sent.Where(s => s.To.Equals(to)).Select(s => s.Message).OfType<T>().ToList();
    }

    /// <summary>forgets everything recorded so far, so a test can check only what happens next</summary>
    public void Clear()
    {
        Sent.Clear();
        Broadcasts.Clear();
    }
}
