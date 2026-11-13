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

    public CommunicatorClient(IPEndPoint serverIp)
    {
        _serverIp = serverIp;
        _tcpClient = new TcpClient();
        _receivingQueueListener = new ReceivingQueueListener(_receivingQueue, _moduleToMessageListenerMap);
        try
        {
            _tcpClient.LingerState = new LingerOption(false, 0);
            _tcpClient.Connect(_serverIp.Address, _serverIp.Port);
            _socketListener = new SocketListener(_receivingQueue, _tcpClient);
            _sendingQueueListenerClient = new SendingQueueListenerClient(_sendingQueue, _tcpClient);

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

    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        _moduleToMessageListenerMap[module] = subscriber;
    }

    public void RemoveSubscriber(int module)
    {
        _moduleToMessageListenerMap.TryRemove(module, out _);
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

    [ExcludeFromCodeCoverage]
    public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
        throw new NotImplementedException();
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
        _socketListener?.Dispose();
        _sendingQueueListenerClient?.Dispose();
        _receivingQueueListener.Dispose();
        _tcpClient.Close();
    }

}
