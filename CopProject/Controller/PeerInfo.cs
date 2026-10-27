namespace Controller;

// Holds the network identity of a session participant so the host
//can send messages back to them (broadcasts, notifications, etc.).
public sealed class PeerInfo
{
    public string UserId { get; }
    public string IpAddress { get; }
    public int Port { get; }

    public PeerInfo(string userId, string ipAddress, int port)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Port = port;
    }

    public override string ToString() => $"{UserId}@{IpAddress}:{Port}";
}
