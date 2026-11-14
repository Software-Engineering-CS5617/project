// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using Networking;
using Utils;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Server;

namespace Whiteboard.Services;

/// <summary>
/// Encapsulates the network session lifecycle for the whiteboard.
/// Manages server startup, client communicator creation, and user presence notifications.
/// </summary>
public sealed partial class WhiteboardSession : IDisposable, IUserNotificationHandler
{
    private readonly ICommunicator? _hostClientCommunicator;
    private bool _disposed;

    /// <summary>
    /// Gets the active whiteboard client for this user.
    /// </summary>
    public IWhiteboardClient Client { get; }

    /// <summary>
    /// Gets the server instance if running as host; otherwise null.
    /// </summary>
    public IWhiteboardServer? Server { get; }

    /// <summary>
    /// Raised when a remote participant leaves the meeting.
    /// </summary>
    public event Action<string>? UserLeft;

    /// <summary>
    /// Raised when a participant joins the meeting.
    /// </summary>
    public event Action<ControllerUser>? UserJoined;

    private WhiteboardSession(IWhiteboardClient client, IWhiteboardServer? server, ICommunicator? hostClientCommunicator)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Server = server;
        _hostClientCommunicator = hostClientCommunicator;
    }

    /// <summary>
    /// Starts a whiteboard session for the host, running a background whiteboard server and a dedicated local client
    /// </summary>
    /// <param name="userId">Current host user ID</param>
    /// <param name="serverCommunicator">Server communicator provided by the meeting controller</param>
    /// <param name="port">Host listening port</param>
    /// <returns>A new <see cref="WhiteboardSession"/> containing both the server and host client</returns>
    public static WhiteboardSession StartHost(string userId, ICommunicator serverCommunicator, int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(serverCommunicator);

        IPAddress hostAddress = serverCommunicator.ListenIpAddress ?? IPAddress.Loopback;
        int hostPort = serverCommunicator.ListenPort != 0 ? serverCommunicator.ListenPort : port;
        IPEndPoint serverEndPoint = new(hostAddress, hostPort);

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(serverCommunicator);
        ICommunicator hostClientComm = CommunicatorFactory.CreateClientCommunicator(serverEndPoint);
        IWhiteboardClient client = WhiteboardFactory.CreateClientWhiteboard(hostClientComm, serverEndPoint, userId);

        return new WhiteboardSession(client, server, hostClientComm);
    }

    /// <summary>
    /// Starts a whiteboard session for a participant
    /// </summary>
    /// <param name="userId">Current participant user ID</param>
    /// <param name="clientCommunicator">Client communicator provided by the meeting controller</param>
    /// <param name="hostIp">Host IP address or hostname</param>
    /// <param name="port">Host listening port</param>
    /// <returns>A new <see cref="WhiteboardSession"/> containing only the client</returns>
    public static WhiteboardSession StartClient(string userId, ICommunicator clientCommunicator, string hostIp, int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(clientCommunicator);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostIp);

        IPAddress ip = hostIp.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            ? IPAddress.Loopback
            : (IPAddress.TryParse(hostIp, out IPAddress? parsed) ? parsed : IPAddress.Loopback);

        IPEndPoint endPoint = new(ip, port);
        IWhiteboardClient client = WhiteboardFactory.CreateClientWhiteboard(clientCommunicator, endPoint, userId);

        return new WhiteboardSession(client, server: null, hostClientCommunicator: null);
    }

    /// <summary>
    /// Starts a new whiteboard session, a host or participant whiteboard session based on <paramref name="isHost"/>
    /// </summary>
    /// <param name="userId">Current user ID</param>
    /// <param name="communicator">Session communicator (server communicator on host, client communicator on participant)</param>
    /// <param name="hostIp">Host IP address or hostname</param>
    /// <param name="port">Host listening port</param>
    /// <param name="isHost">True iff the local user is hosting</param>
    /// <returns>A new <see cref="WhiteboardSession"/></returns>
    public static WhiteboardSession Start(string userId, ICommunicator communicator, string hostIp, int port, bool isHost)
    {
        return isHost
            ? StartHost(userId, communicator, port)
            : StartClient(userId, communicator, hostIp, port);
    }

    public void OnUserJoined(ControllerUser user)
    {
        if (user == null)
        {
            return;
        }

        Trace.WriteLine($"[WhiteboardSession] User joined: {user.Username}");
        UserJoined?.Invoke(user);
    }

    public void OnUserLeft(ControllerUser user)
    {
        if (user == null)
        {
            return;
        }

        Trace.WriteLine($"[WhiteboardSession] User left: {user.Username}");
        UserLeft?.Invoke(user.Username);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        (Client as IDisposable)?.Dispose();
        (Server as IDisposable)?.Dispose();
        (_hostClientCommunicator as IDisposable)?.Dispose();
    }
}
