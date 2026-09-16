using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Networking;


/// <summary>
/// Communicator that can send and listen for messages over the network using UDP.
/// </summary>
internal class UdpCommunicator : ICommunicator
{
    private readonly UdpClient _listener;
    private readonly Thread _listenThread;      // Thread that listens for messages on the UDP port.
    private readonly Dictionary<string, IMessageListener> _subscribers; // List of subscribers.

    public UdpCommunicator()
    {
        _subscribers = [];

        ListenPort = GetRandomAvailablePort(); ;
        _listener = new(ListenPort);
        _listenThread = new(new ThreadStart(ListenerThreadProc))
        {
            IsBackground = true // Stop the thread when the main thread stops.
        };
        _listenThread.Start();
        Trace.TraceInformation($"Udp communicator listening on port {ListenPort}");
    }

    /// <inheritdoc />
    public int ListenPort { get; private set; }

    /// <inheritdoc />
    public void AddSubscriber(string id, IMessageListener subscriber)
    {
        Debug.Assert(!string.IsNullOrEmpty(id));
        Debug.Assert(subscriber != null);

        lock (this)
        {
            if (_subscribers.ContainsKey(id))
            {
                _subscribers[id] = subscriber;
            }
            else
            {
                _subscribers.Add(id, subscriber);
            }
        }
    }

    /// <inheritdoc />
    public void RemoveSubscriber(string id)
    {
        Debug.Assert(!string.IsNullOrEmpty(id));
        lock (this)
        {
            _ = _subscribers.Remove(id);
        }
    }

    /// <inheritdoc/>
    public void SendMessage(string ipAddress, int port, string senderId, string message)
    {
        Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress broadcastAddress = IPAddress.Parse(ipAddress);
        byte[] sendBuffer = Encoding.ASCII.GetBytes($"{senderId}:{message}");
        IPEndPoint endPoint = new(broadcastAddress, port);
        int bytesSent = socket.SendTo(sendBuffer, endPoint);
        Debug.Assert(bytesSent == sendBuffer.Length);
    }

    /// <summary>
    /// Gets a random available port.
    /// </summary>
    /// <returns>An available port</returns>
    private static int GetRandomAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Listens for messages on the listening port.
    /// </summary>
    private void ListenerThreadProc()
    {
        Trace.TraceInformation($"Listener Thread Id = {Environment.CurrentManagedThreadId}.");
        IPEndPoint endPoint = null!;
        while (true)
        {
            try
            {
                // Listen for message on the listening port, and receive it when it comes along.
                byte[] bytes = _listener.Receive(ref endPoint);
                string payload = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                Trace.TraceInformation($"Received payload: {payload}");

                // The received payload is expected to be in the format <Identity>:<Message>
                string[] tokens = payload.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 2)
                {
                    string id = tokens[0];
                    string message = tokens[1];
                    lock (this)
                    {
                        if (_subscribers.ContainsKey(id))
                        {
                            _subscribers[id].OnMessageReceived(message);
                        }
                        else
                        {
                            Trace.TraceWarning($"Received message for unknown subscriber: {id}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
            }
        }
    }
}