using System.Diagnostics;
using System.Net;
using Networking;

namespace ScreenShare;

/// <summary>
/// wraps the meeting's ICommunicator so ScreenShare's traffic can be observed and a failing
/// peer cannot hurt the others. other modules' traffic passes through untouched.
///
/// what it adds, for ScreenShare's module only:
/// - every inbound and outbound message is shown to the ITrafficObserver
/// - sends to peers the observer declared gone are dropped. Networking has a single sending
///   thread for all clients, so every packet queued for a dead peer is a chance to stall
///   the sends to everyone else.
/// - a send that throws is swallowed and reported. the shipped CommunicatorServer and
///   CommunicatorClient never throw from SendMessage (they only enqueue and log), so today this
///   is a safety net for other ICommunicator implementations, e.g. test doubles.
///
/// the three multicast members are passed straight through; ScreenShare does not use them.
/// </summary>
public sealed class ResilientCommunicator : ICommunicator
{
    private readonly ICommunicator _inner;
    private readonly ITrafficObserver _observer;
    private volatile IMessageListener? _screenShareListener;

    public ResilientCommunicator(ICommunicator inner, ITrafficObserver observer)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(observer);

        _inner = inner;
        _observer = observer;
    }

    public IPAddress ListenIpAddress => _inner.ListenIpAddress;

    public int ListenPort => _inner.ListenPort;

    public void AddSubscriber(int moduleId, IMessageListener listener)
    {
        if (moduleId != ScreenShareModule.ModuleId)
        {
            _inner.AddSubscriber(moduleId, listener);
            return;
        }

        _screenShareListener = listener;
        _inner.AddSubscriber(moduleId, new ObservingListener(listener, _observer));
    }

    public void RemoveSubscriber(int moduleId)
    {
        if (moduleId == ScreenShareModule.ModuleId)
        {
            _screenShareListener = null;
        }

        _inner.RemoveSubscriber(moduleId);
    }

    public void SendMessage(IPEndPoint to, byte[] data, int moduleId, int priority)
    {
        if (moduleId != ScreenShareModule.ModuleId)
        {
            _inner.SendMessage(to, data, moduleId, priority);
            return;
        }

        Observe(() => _observer.OnOutbound(to, data));

        if (!_observer.ShouldSend(to))
        {
            return;
        }

        try
        {
            _inner.SendMessage(to, data, moduleId, priority);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[ScreenShare] send to {to} failed: {e.Message}");
        }
    }

    public void BroadcastMessage(byte[] data, int moduleId, int priority)
    {
        if (moduleId != ScreenShareModule.ModuleId)
        {
            _inner.BroadcastMessage(data, moduleId, priority);
            return;
        }

        try
        {
            _inner.BroadcastMessage(data, moduleId, priority);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[ScreenShare] broadcast failed: {e.Message}");
        }
    }

    public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        _inner.JoinMulticastGroup(multicastGroup, clientIp);
    }

    public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        _inner.LeaveMulticastGroup(multicastGroup, clientIp);
    }

    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
        _inner.MulticastMessage(multicastGroup, message, module, priority);
    }

    /// <summary>
    /// hands ScreenShare a message as if the network had delivered it, e.g. a StreamStopped
    /// for a stream that ended while this client was offline
    /// </summary>
    public void InjectLocal(IScreenShareMessage message)
    {
        _screenShareListener?.OnMessageReceived(ScreenShareCodec.Encode(message));
    }

    // watching must never get in the way of delivering
    private static void Observe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[ScreenShare] traffic observer failed: {e}");
        }
    }

    private sealed class ObservingListener : IMessageListener
    {
        private readonly IMessageListener _inner;
        private readonly ITrafficObserver _observer;

        public ObservingListener(IMessageListener inner, ITrafficObserver observer)
        {
            _inner = inner;
            _observer = observer;
        }

        public void OnMessageReceived(byte[] message)
        {
            Observe(() => _observer.OnInbound(message));
            _inner.OnMessageReceived(message);
        }
    }
}
