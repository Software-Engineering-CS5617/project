using System.Diagnostics;

namespace Networking;

/// <summary>
/// Factory for creating a communicator.
/// </summary>
public static class CommunicatorFactory
{
    /// <summary>
    /// Creates a communicator.
    /// </summary>
    /// <returns>A new communicator instance</returns>
    public static ICommunicator CreateCommunicator()
    {
        ICommunicator communicator = new UdpCommunicator();
        Trace.TraceInformation($"Starting Udp Communicator in port {communicator.ListenPort}");
        return communicator;
    }
}