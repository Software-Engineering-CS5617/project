/// Owners  : Raagam Hitesh Parmar 
/// Roll No : 112301024
/// LICENSE : MIT

using System.Net;
using Networking;
using Whiteboard.Client;
using Whiteboard.Server;

namespace Whiteboard;

/// <summary>
/// Factory to get <see cref="IWhiteboardServer"/> and create new <see cref="IWhiteboardClient"/>
/// </summary>
public static class WhiteboardFactory
{
    /// <summary>
    /// Create a new whiteboard client
    /// </summary>
    /// <returns>A new whiteboard client state manager</returns>
    public static IWhiteboardClient CreateClientWhiteboard(ICommunicator clientCommunicator, IPEndPoint serverEndPoint, string userId)
    {
        return new WhiteboardClient(clientCommunicator, serverEndPoint, userId);
    }

    /// <summary>
    /// Create a new whiteboard server
    /// </summary>
    /// <returns>A new whiteboard server state manager</returns>
    public static IWhiteboardServer GetServerWhiteboard(ICommunicator serverCommunicator)
    {
        return new WhiteboardServer(serverCommunicator);
    }
}
