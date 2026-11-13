using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;
using Networking.Sockets;

namespace Networking.TCPCommunication;

public class CommunicatorServer : ICommunicator, IDisposable
{
    public const int NetworkingModuleId = 13;
    public const int NetworkModuleId = 13;

    private SendingQueue _sendingQueue = new SendingQueue();
    private ReceivingQueue _receivingQueue = new ReceivingQueue();
    private SendingQueueListenerServer? _sendingQueueListenerServer;
    private ReceivingQueueListener? _receivingQueueListener;
    private TcpListener? _tcpClientRequestListener;
    private ConcurrentDictionary<IPEndPoint, TcpClient> _clientToTcpListenerMap = new ConcurrentDictionary<IPEndPoint, TcpClient>();
    private ConcurrentDictionary<int, IMessageListener> _moduleToMessageListenerMap = new ConcurrentDictionary<int, IMessageListener>();
    private ConcurrentDictionary<IPEndPoint, SocketListener> _clientToSocketListenerMap = new ConcurrentDictionary<IPEndPoint, SocketListener>();
    private ConcurrentDictionary<int, ConcurrentDictionary<IPEndPoint, byte>> _multicastGroupMap = new ConcurrentDictionary<int, ConcurrentDictionary<IPEndPoint, byte>>();

    private UdpClient? _udpClient;
    private SendingQueue _sendingQueueLossy = new SendingQueue();
    private ReceivingQueue _receivingQueueLossy = new ReceivingQueue();
    private SendingQueueListenerLossy? _sendingQueueListenerLossy;
    private ReceivingQueueListenerLossy? _receivingQueueListenerLossy;
    private UdpSocketListener? _udpSocketListener;
    private ConcurrentDictionary<int, IMessageListenerLossy> _moduleToLossyMessageListenerMap = new ConcurrentDictionary<int, IMessageListenerLossy>();

    private bool _isRunning;
    private bool _disposed;
    private object _isRunningLock;
    private IPEndPoint? _thisIpEndPoint;

    private Thread? _clientConnectionListenerThread;
    private bool IsRunning
    {
        get {
            lock (_isRunningLock)
            {
                return _isRunning;
            }
        }
    }

    public CommunicatorServer(IPEndPoint? ipEndPoint)
    {
        _isRunningLock = new object();
        try
        {
            if (ipEndPoint == null)
            {
                ipEndPoint = GetFreeIPEndPoint();
            }
            _thisIpEndPoint = ipEndPoint;
            _tcpClientRequestListener = new TcpListener(ipEndPoint.Address, ipEndPoint.Port);
            _tcpClientRequestListener.Start();

            _sendingQueueListenerServer = new SendingQueueListenerServer(_sendingQueue, _clientToTcpListenerMap);
            _receivingQueueListener = new ReceivingQueueListener(_receivingQueue, _moduleToMessageListenerMap);

            try
            {
                _udpClient = new UdpClient(new IPEndPoint(ipEndPoint.Address, ipEndPoint.Port));
                _sendingQueueListenerLossy = new SendingQueueListenerLossy(_sendingQueueLossy, _udpClient);
                _udpSocketListener = new UdpSocketListener(_receivingQueueLossy, _udpClient);
                _receivingQueueListenerLossy = new ReceivingQueueListenerLossy(_receivingQueueLossy, _moduleToLossyMessageListenerMap);
            }
            catch (Exception udpEx)
            {
                Trace.WriteLine($"[Networking] CommunicatorServer: Unable to start UDP server on {ipEndPoint}. Exception: {udpEx.Message}");
            }

            AddSubscriber(NetworkingModuleId, new NetworkMessageListener(this));
            AddLossySubscriber(NetworkingModuleId, new LossyNetworkMessageListener(this));

            _isRunning = true;
            _clientConnectionListenerThread = new Thread(AcceptClientConnections) { IsBackground = true };
            _clientConnectionListenerThread.Start();
            // using (_tcpClientRequestListener)
            // using (_sendingQueueListenerServer)
            // using (_receivingQueueListener)
            // {
            //     _isRunning = true;
            //     _clientConnectionListenerThread.Start();
            // }
        }
        catch (Exception e)
        {
            _thisIpEndPoint = null;
            _tcpClientRequestListener = null;
            Trace.WriteLine($"[Networking] CommunicatorServer.CommunicatorServer: Unable to start the TCP server. Exception: {e.Message}");
        }
    }


    public void AcceptClientConnections()
    {
        TcpListener? listener = _tcpClientRequestListener;
        if (listener == null)
        {
            return;
        }
        while (IsRunning)
        {
            try
            {
                TcpClient clientTcpClient = listener.AcceptTcpClient();
                if (clientTcpClient.Client.RemoteEndPoint is IPEndPoint clientEp)
                {
                    if (clientEp.Address.IsIPv4MappedToIPv6)
                    {
                        clientEp = new IPEndPoint(clientEp.Address.MapToIPv4(), clientEp.Port);
                    }
                    AddClient(clientEp, clientTcpClient);
                }
            }
            catch (Exception e) when (e is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                Trace.WriteLine($"[Networking] CommunicatorServer: TCP Listener Socket stopped or closed: {e.Message}");
                break;
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[Networking] CommunicatorServer.AcceptClientConnections unexpected exception: {e.Message}");
                if (!IsRunning)
                {
                    break;
                }
            }
        }

    }

    public IPEndPoint GetFreeIPEndPoint()
    {
        IPAddress ipAddress = IPAddress.Loopback;
        try
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork) ?? IPAddress.Loopback;
        }
        catch
        {
            ipAddress = IPAddress.Loopback;
        }
        using TcpListener dummyListener = new TcpListener(IPAddress.Loopback, 0);
        dummyListener.Start();
        int port = ((IPEndPoint)dummyListener.LocalEndpoint).Port;
        dummyListener.Stop();
        return new IPEndPoint(ipAddress, port);
    }

    public void AddClient(IPEndPoint clientIp, TcpClient tcpClient)
    {
        try
        {
            _clientToTcpListenerMap[clientIp] = tcpClient;
            _clientToSocketListenerMap[clientIp] = new SocketListener(_receivingQueue, tcpClient);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorServer.AddClient: Failed to add client {clientIp}. Exception: {e.Message}");
        }
    }

    public void RemoveClient(IPEndPoint clientIp)
    {
        try
        {
            SocketListener socketListener = _clientToSocketListenerMap[clientIp];
            _clientToSocketListenerMap.TryRemove(clientIp, out _);
            _clientToTcpListenerMap.TryRemove(clientIp, out _);
            foreach (ConcurrentDictionary<IPEndPoint, byte> group in _multicastGroupMap.Values)
            {
                group.TryRemove(clientIp, out _);
            }
            socketListener.Dispose();
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorServer.RemoveClient: Failed to remove client {clientIp}. Exception: {e.Message}");
        }
    }

    public void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
    {
        try
        {
            Packet packet = new Packet(message, iPEndPoint, module, priority);
            _sendingQueue.Enqueue(packet);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorServer.SendMessage: Failed to enqueue message for destination {iPEndPoint}. Exception: {e.Message}");
        }
    }

    public void AddSubscriber(int module, IMessageListener messageListener)
    {
        try
        {
            _moduleToMessageListenerMap.TryAdd(module, messageListener);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorServer.AddSubscriber: Failed to add subscriber for module {module}. Exception: {e.Message}");
        }
    }
    public void RemoveSubscriber(int module)
    {
        try
        {
            _moduleToMessageListenerMap.TryRemove(module, out _);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorServer.RemoveSubscriber: Failed to remove subscriber for module {module}. Exception: {e.Message}");
        }
    }

    public void SendMessageLossy(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
    {
        try
        {
            Packet packet = new Packet(message, iPEndPoint, module, priority);
            _sendingQueueLossy.Enqueue(packet);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorServer.SendMessageLossy: Failed to enqueue lossy message for destination {iPEndPoint}. Exception: {e.Message}");
        }
    }

    public void AddLossySubscriber(int module, IMessageListenerLossy messageListener)
    {
        try
        {
            _moduleToLossyMessageListenerMap.TryAdd(module, messageListener);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorServer.AddLossySubscriber: Failed to add subscriber for module {module}. Exception: {e.Message}");
        }
    }

    public void RemoveLossySubscriber(int module)
    {
        try
        {
            _moduleToLossyMessageListenerMap.TryRemove(module, out _);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorServer.RemoveLossySubscriber: Failed to remove subscriber for module {module}. Exception: {e.Message}");
        }
    }


    public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        if (clientIp.Address.IsIPv4MappedToIPv6)
        {
            clientIp = new IPEndPoint(clientIp.Address.MapToIPv4(), clientIp.Port);
        }

        ConcurrentDictionary<IPEndPoint, byte> group = _multicastGroupMap.GetOrAdd(
            multicastGroup,
            _ => new ConcurrentDictionary<IPEndPoint, byte>()
        );
        group.TryAdd(clientIp, 0);
    }

    public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        if (clientIp.Address.IsIPv4MappedToIPv6)
        {
            clientIp = new IPEndPoint(clientIp.Address.MapToIPv4(), clientIp.Port);
        }

        if (_multicastGroupMap.TryGetValue(multicastGroup, out ConcurrentDictionary<IPEndPoint, byte>? group))
        {
            foreach (IPEndPoint ep in group.Keys)
            {
                if (EndPointsEqual(ep, clientIp))
                {
                    group.TryRemove(ep, out _);
                }
            }
            if (group.IsEmpty)
            {
                _multicastGroupMap.TryRemove(multicastGroup, out _);
            }
        }
    }



    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
        MulticastMessage(multicastGroup, message, module, priority, null);
    }

    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority, IPEndPoint? excludeClient)
    {
        if (_multicastGroupMap.TryGetValue(multicastGroup, out ConcurrentDictionary<IPEndPoint, byte>? group))
        {
            foreach (IPEndPoint clientEp in group.Keys)
            {
                if (excludeClient != null && EndPointsEqual(clientEp, excludeClient))
                {
                    continue;
                }
                SendMessage(clientEp, message, module, priority);
            }
        }
    }

    public void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority)
    {
        MulticastMessageLossy(multicastGroup, message, module, priority, null);
    }

    public void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority, IPEndPoint? excludeClient)
    {
        if (_multicastGroupMap.TryGetValue(multicastGroup, out ConcurrentDictionary<IPEndPoint, byte>? group))
        {
            foreach (IPEndPoint clientEp in group.Keys)
            {
                if (excludeClient != null && EndPointsEqual(clientEp, excludeClient))
                {
                    continue;
                }
                SendMessageLossy(clientEp, message, module, priority);
            }
        }
    }

    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        BroadcastMessage(message, module, priority, null);
    }

    public void BroadcastMessage(byte[] message, int module, int priority, IPEndPoint? excludeClient)
    {
        foreach (IPEndPoint clientEp in _clientToTcpListenerMap.Keys)
        {
            if (excludeClient != null && EndPointsEqual(clientEp, excludeClient))
            {
                continue;
            }
            SendMessage(clientEp, message, module, priority);
        }
    }

    public void BroadcastMessageLossy(byte[] message, int module, int priority)
    {
        BroadcastMessageLossy(message, module, priority, null);
    }

    public void BroadcastMessageLossy(byte[] message, int module, int priority, IPEndPoint? excludeClient)
    {
        foreach (IPEndPoint clientEp in _clientToTcpListenerMap.Keys)
        {
            if (excludeClient != null && EndPointsEqual(clientEp, excludeClient))
            {
                continue;
            }
            SendMessageLossy(clientEp, message, module, priority);
        }
    }

    private static bool EndPointsEqual(IPEndPoint a, IPEndPoint b)
    {
        if (a.Port != b.Port)
        {
            return false;
        }

        IPAddress addrA = a.Address.IsIPv4MappedToIPv6 ? a.Address.MapToIPv4() : a.Address;
        IPAddress addrB = b.Address.IsIPv4MappedToIPv6 ? b.Address.MapToIPv4() : b.Address;

        return addrA.Equals(addrB);
    }

    public class NetworkMessageListener : IMessageListener
    {
        private readonly CommunicatorServer _server;
        private readonly Serializer<NetworkMessage> _serializer;

        public NetworkMessageListener(CommunicatorServer server)
        {
            _server = server;
            _serializer = new Serializer<NetworkMessage>();
        }

        public void OnMessageReceived(byte[] message)
        {
            try
            {
                NetworkMessage? packet = _serializer.Deserialize(message);
                switch (packet)
                {
                    case BroadcastPacket broadcast:
                        _server.BroadcastMessage(broadcast.Message, broadcast.Module, broadcast.Priority, broadcast.ClientIp);
                        break;
                    case MulticastPacket multicast:
                        _server.MulticastMessage(multicast.MulticastGroup, multicast.Message, multicast.Module, multicast.Priority);
                        break;
                    case JoinGroupPacket join:
                        _server.JoinMulticastGroup(join.MulticastGroup, join.ClientIp);
                        break;
                    case LeaveGroupPacket leave:
                        _server.LeaveMulticastGroup(leave.MulticastGroup, leave.ClientIp);
                        break;
                    default:
                        Trace.WriteLine("[Networking] CommunicatorServer.NetworkMessageListener: Unknown or null message type.");
                        break;
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[Networking] CommunicatorServer.NetworkMessageListener: Failed to process message. Exception: {e.Message}");
            }
        }
    }

    public class LossyNetworkMessageListener : IMessageListenerLossy
    {
        private readonly CommunicatorServer _server;
        private readonly Serializer<NetworkMessage> _serializer;

        public LossyNetworkMessageListener(CommunicatorServer server)
        {
            _server = server;
            _serializer = new Serializer<NetworkMessage>();
        }

        public void OnMessageReceivedLossy(byte[] message)
        {
            try
            {
                NetworkMessage? packet = _serializer.Deserialize(message);
                switch (packet)
                {
                    case BroadcastPacket broadcast:
                        _server.BroadcastMessageLossy(broadcast.Message, broadcast.Module, broadcast.Priority, broadcast.ClientIp);
                        break;
                    case MulticastPacket multicast:
                        _server.MulticastMessageLossy(multicast.MulticastGroup, multicast.Message, multicast.Module, multicast.Priority);
                        break;
                    default:
                        Trace.WriteLine("[Networking] CommunicatorServer.LossyNetworkMessageListener: Unknown or null message type.");
                        break;
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[Networking] CommunicatorServer.LossyNetworkMessageListener: Failed to process lossy message. Exception: {e.Message}");
            }
        }
    }

    public int ListenPort => _thisIpEndPoint?.Port ?? 0;

    public IPAddress ListenIpAddress => _thisIpEndPoint?.Address ?? IPAddress.Any;

    public void Dispose()
    {
        lock (_isRunningLock)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _isRunning = false;
        }

        _sendingQueueListenerServer?.Dispose();
        _sendingQueueListenerLossy?.Dispose();
        _tcpClientRequestListener?.Stop();
        _receivingQueueListener?.Dispose();
        _receivingQueueListenerLossy?.Dispose();
        _udpSocketListener?.Dispose();
        _udpClient?.Dispose();

        foreach (TcpClient tcpClient in _clientToTcpListenerMap.Values)
        {
            try
            {
                tcpClient.Close();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[Networking] CommunicatorServer.Dispose: Error closing client TcpClient. Exception: {e.Message}");
            }
        }

        foreach (SocketListener socketListener in _clientToSocketListenerMap.Values)
        {
            socketListener.Dispose();
        }
        _clientToTcpListenerMap.Clear();
        _clientToSocketListenerMap.Clear();
        _multicastGroupMap.Clear();
        _moduleToMessageListenerMap.Clear();
        _moduleToLossyMessageListenerMap.Clear();

        _sendingQueue.Dispose();
        _receivingQueue.Dispose();
        _sendingQueueLossy.Dispose();
        _receivingQueueLossy.Dispose();
    }
}

