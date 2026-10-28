using System.Diagnostics;
using System.Net;
using Networking.TCPCommunication;

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
    public static ICommunicator? CreateCommunicator(IPEndPoint? serverIp, bool isServer = true, bool isClient = false)
    {
        if (isServer == isClient)
        {
            Trace.WriteLine("[Networking] CommunicatorFactory.CreateCommunicator: Ambiguity in creation of Communicator, isServer == isClient");
        }
        else if (isServer)
        {
            return new CommunicatorServer(null);
        }
        else
        {
            if (serverIp != null)
            {
                return new CommunicatorClient(serverIp);
            }
            else
            {
                Trace.WriteLine("[Networking] CommunicatorFactory.CreateCommunicator: Unable to start Client Communicator, Server IP is not provided");
            }
        }
        return null;
    }
}
