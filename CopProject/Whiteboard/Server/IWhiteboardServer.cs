/// Owner   : Raagam Hitesh Parmar 
/// Roll No : 112301024
/// LICENSE : MIT

using Networking;
using Whiteboard.Interfaces;

namespace Whiteboard.Server;

/// <summary>
/// Whiteboard server state manager
/// </summary>
public interface IWhiteboardServer : IMessageListener
{
    /// <summary>
    /// Server whiteboard state
    /// </summary>
    IWhiteboard Canvas { get; }
}
