using System.Net;
using System.Net.Sockets;

namespace Controller;

// Utility for resolving the local machine's network IP address.
public static class NetworkUtils
{
    //Returns the local machine's IP address on the network.
    // Uses a UDP connect trick to determine which interface would be used
       
    public static string GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // Connect to a public address — no data is sent over UDP,
            // but the OS selects the correct local interface.
            socket.Connect("8.8.8.8", 80);
            return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
        }
        catch
        {
            // Fallback for machines with no network access
            return "127.0.0.1";
        }
    }
}
