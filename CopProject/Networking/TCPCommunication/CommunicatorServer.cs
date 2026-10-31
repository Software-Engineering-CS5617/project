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
    private SendingQueue _sendingQueue = new SendingQueue();
    private ReceivingQueue _receivingQueue = new ReceivingQueue();
    private SendingQueueListenerServer? _sendingQueueListenerServer;
    private ReceivingQueueListener? _receivingQueueListener;
    private TcpListener? _tcpClientRequestListener;
    private ConcurrentDictionary<IPEndPoint, TcpClient> _clientToTcpListenerMap = new ConcurrentDictionary<IPEndPoint, TcpClient>();
    private ConcurrentDictionary<int, IMessageListener> _moduleToMessageListenerMap = new ConcurrentDictionary<int, IMessageListener>();
    private ConcurrentDictionary<IPEndPoint, SocketListener> _clientToSocketListenerMap = new ConcurrentDictionary<IPEndPoint, SocketListener>();
    private ConcurrentDictionary<int, ConcurrentBag<IPEndPoint>> _multicastGroupMap = new ConcurrentDictionary<int, ConcurrentBag<IPEndPoint>>();
    private bool _isRunning;
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
                    AddClient(clientEp, clientTcpClient);
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.Interrupted)
                {
                    Trace.WriteLine("[Networking] Error in CommunicatorServer in connecting to TCP Listener Socket. Socket has been closed");
                }
                else
                {
                    Trace.WriteLine($"[Networking] Error in CommunicatorServer.AcceptClientConnections. Exception: {e.Message}");
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


    public void JoinMulticastGroup(int multicastGroup)
    {
        throw new NotImplementedException();
    }
    public void LeaveMulticastGroup(int multicastGroup)
    {
        throw new NotImplementedException();
    }
    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
        throw new NotImplementedException();
    }
    public void BroadcastMessage(byte[] message, int module, int priority)
    {
        foreach (IPEndPoint clientEp in _clientToTcpListenerMap.Keys)
        {
            SendMessage(clientEp, message, module, priority);
        }
    }

    public int ListenPort => _thisIpEndPoint?.Port ?? 0;

    public IPAddress ListenIpAddress => _thisIpEndPoint?.Address ?? IPAddress.Any;

    public void Dispose()
    {
        lock (_isRunningLock)
        {
            _isRunning = false;
        }
        _tcpClientRequestListener?.Stop();
        _sendingQueueListenerServer?.Dispose();
        _receivingQueueListener?.Dispose();
        foreach (SocketListener socketListener in _clientToSocketListenerMap.Values)
        {
            socketListener.Dispose();
        }
    }
}

