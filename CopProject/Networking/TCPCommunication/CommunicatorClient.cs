using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;
using Networking.Queues;
using Networking.Sockets;

namespace Networking.TCPCommunication;

public class CommunicatorClient : ICommunicator, IDisposable
{
    private readonly SendingQueue _sendingQueue = new SendingQueue();
    private readonly ReceivingQueue _receivingQueue = new ReceivingQueue();
    private readonly SocketListener? _socketListener;
    private readonly SendingQueueListenerClient? _sendingQueueListenerClient;
    private readonly ReceivingQueueListener _receivingQueueListener;
    private readonly TcpClient _tcpClient;
    private readonly IPEndPoint _serverIp;
    private readonly ConcurrentDictionary<int, IMessageListener> _moduleToMessageListenerMap = new ConcurrentDictionary<int, IMessageListener>();
    private readonly Serializer<NetworkMessage> _networkMessageSerializer = new Serializer<NetworkMessage>();

    private UdpClient? _udpClient;
    private UdpClient? _multicastUdpClient;
    private readonly SendingQueue _sendingQueueLossy = new SendingQueue();
    private readonly ReceivingQueue _receivingQueueLossy = new ReceivingQueue();
    private readonly SendingQueueListenerLossy? _sendingQueueListenerLossy;
    private readonly ReceivingQueueListenerLossy _receivingQueueListenerLossy;
    private readonly UdpSocketListener? _udpSocketListener;
    private readonly UdpSocketListener? _multicastSocketListener;
    private readonly ConcurrentDictionary<int, IMessageListenerLossy> _moduleToLossyMessageListenerMap = new ConcurrentDictionary<int, IMessageListenerLossy>();

    public CommunicatorClient(IPEndPoint serverIp)
    {
        _serverIp = serverIp;
        _tcpClient = new TcpClient();
        _receivingQueueListener = new ReceivingQueueListener(_receivingQueue, _moduleToMessageListenerMap);
        _receivingQueueListenerLossy = new ReceivingQueueListenerLossy(_receivingQueueLossy, _moduleToLossyMessageListenerMap);
        try
        {
            _tcpClient.LingerState = new LingerOption(false, 0);
            _tcpClient.Connect(_serverIp.Address, _serverIp.Port);
            _socketListener = new SocketListener(_receivingQueue, _tcpClient);
            _sendingQueueListenerClient = new SendingQueueListenerClient(_sendingQueue, _tcpClient);

            int localPort = ListenPort;
            Socket clientUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientUdpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            clientUdpSocket.Bind(new IPEndPoint(IPAddress.Any, localPort));
            _udpClient = new UdpClient {
                Client = clientUdpSocket,
                EnableBroadcast = true,
                MulticastLoopback = true
            };

            try
            {
                Socket multicastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                multicastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                multicastSocket.Bind(new IPEndPoint(IPAddress.Any, _serverIp.Port));
                _multicastUdpClient = new UdpClient {
                    Client = multicastSocket,
                    EnableBroadcast = true,
                    MulticastLoopback = true
                };
                _multicastSocketListener = new UdpSocketListener(_receivingQueueLossy, _multicastUdpClient);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[Networking] CommunicatorClient: Failed to initialize multicast UDP listener on port {_serverIp.Port}: {ex.Message}");
            }

            _sendingQueueListenerLossy = new SendingQueueListenerLossy(_sendingQueueLossy, _udpClient);
            _udpSocketListener = new UdpSocketListener(_receivingQueueLossy, _udpClient);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.CommunicatorClient: Failed to connect to server at {serverIp}. Exception: {e.Message}");
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
            Trace.WriteLine($"[Networking] CommunicatorClient.SendMessage: Failed to enqueue message for destination {iPEndPoint}. Exception: {e.Message}");
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
            Trace.WriteLine($"[Networking] CommunicatorClient.SendMessageLossy: Failed to enqueue lossy message for destination {iPEndPoint}. Exception: {e.Message}");
        }
    }

    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        _moduleToMessageListenerMap[module] = subscriber;
    }

    public void RemoveSubscriber(int module)
    {
        _moduleToMessageListenerMap.TryRemove(module, out _);
    }

    public void AddLossySubscriber(int module, IMessageListenerLossy subscriber)
    {
        _moduleToLossyMessageListenerMap[module] = subscriber;
    }

    public void RemoveLossySubscriber(int module)
    {
        _moduleToLossyMessageListenerMap.TryRemove(module, out _);
    }

    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        try
        {
            IPEndPoint clientEndpoint = new IPEndPoint(ListenIpAddress, ListenPort);
            BroadcastPacket packet = new BroadcastPacket(message, module, priority, clientEndpoint);
            byte[] serialized = _networkMessageSerializer.Serialize(packet);
            SendMessage(_serverIp, serialized, CommunicatorServer.NetworkingModuleId, priority);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.BroadcastMessage: Failed to broadcast message. Exception: {e.Message}");
        }
    }

    public void BroadcastMessageLossy(byte[] message, int module, int priority)
    {
        try
        {
            IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, _serverIp.Port);
            SendMessageLossy(broadcastEndpoint, message, module, priority);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.BroadcastMessageLossy: Failed to broadcast lossy message. Exception: {e.Message}");
        }
    }

    public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        try
        {
            try
            {
                IPAddress groupAddress = CommunicatorServer.GetMulticastAddress(multicastGroup);
                _multicastUdpClient?.JoinMulticastGroup(groupAddress);
            }
            catch (Exception mEx)
            {
                Trace.WriteLine($"[Networking] CommunicatorClient.JoinMulticastGroup: Native join failed: {mEx.Message}");
            }

            JoinGroupPacket packet = new JoinGroupPacket(multicastGroup, clientIp);
            byte[] serialized = _networkMessageSerializer.Serialize(packet);
            SendMessage(_serverIp, serialized, CommunicatorServer.NetworkingModuleId, 1);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.JoinMulticastGroup: Failed to join group. Exception: {e.Message}");
        }
    }

    public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        try
        {
            try
            {
                IPAddress groupAddress = CommunicatorServer.GetMulticastAddress(multicastGroup);
                _multicastUdpClient?.DropMulticastGroup(groupAddress);
            }
            catch (Exception mEx)
            {
                Trace.WriteLine($"[Networking] CommunicatorClient.LeaveMulticastGroup: Native drop failed: {mEx.Message}");
            }

            LeaveGroupPacket packet = new LeaveGroupPacket(multicastGroup, clientIp);
            byte[] serialized = _networkMessageSerializer.Serialize(packet);
            SendMessage(_serverIp, serialized, CommunicatorServer.NetworkingModuleId, 1);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.LeaveMulticastGroup: Failed to leave group. Exception: {e.Message}");
        }
    }

    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
        try
        {
            IPEndPoint clientEndpoint = new IPEndPoint(ListenIpAddress, ListenPort);
            MulticastPacket packet = new MulticastPacket(multicastGroup, message, module, priority, clientEndpoint);
            byte[] serialized = _networkMessageSerializer.Serialize(packet);
            SendMessage(_serverIp, serialized, CommunicatorServer.NetworkingModuleId, priority);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.MulticastMessage: Failed to multicast message. Exception: {e.Message}");
        }
    }

    public void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority)
    {
        try
        {
            IPAddress groupAddress = CommunicatorServer.GetMulticastAddress(multicastGroup);
            IPEndPoint multicastEndpoint = new IPEndPoint(groupAddress, _serverIp.Port);
            SendMessageLossy(multicastEndpoint, message, module, priority);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] CommunicatorClient.MulticastMessageLossy: Failed to multicast lossy message. Exception: {e.Message}");
        }
    }

    public int ListenPort
    {
        get {
            if (_tcpClient?.Client?.LocalEndPoint is IPEndPoint ep)
            {
                return ep.Port;
            }

            return 0;
        }
    }

    public IPAddress ListenIpAddress
    {
        get {
            if (_tcpClient?.Client?.LocalEndPoint is IPEndPoint ep)
            {
                if (ep.Address.IsIPv4MappedToIPv6)
                {
                    return ep.Address.MapToIPv4();
                }
                return ep.Address;
            }

            return IPAddress.Any;
        }
    }


    public void Dispose()
    {
        _sendingQueueListenerClient?.Dispose();
        _sendingQueueListenerLossy?.Dispose();
        _socketListener?.Dispose();
        _receivingQueueListener.Dispose();
        _receivingQueueListenerLossy.Dispose();
        _multicastSocketListener?.Dispose();
        _multicastUdpClient?.Dispose();
        _udpSocketListener?.Dispose();
        _udpClient?.Dispose();
        _tcpClient.Close();
        _sendingQueue.Dispose();
        _receivingQueue.Dispose();
        _sendingQueueLossy.Dispose();
        _receivingQueueLossy.Dispose();
    }

}
