// Author : Eswar

using System.Net;
using Networking;
using Utils;

namespace FileSynchroniser.Network;

/// <param name="Communicator">the meeting's existing communicator (never create a new one)</param>
/// <param name="IsHost">true when this participant runs the server</param>
/// <param name="LocalEndpoint">this user's Ip:Port exactly as the meeting's user list shows it</param>
/// <param name="HostEndpoint">the host's endpoint; required for clients, null for the host</param>
/// <param name="InitialPeers">participants already in the meeting, without the local user</param>
/// <param name="RegisterHandler">controller callback for live join/leave updates</param>
/// <param name="UnregisterHandler">undoes RegisterHandler</param>
public sealed record FileSyncSession(
    ICommunicator Communicator,
    bool IsHost,
    IPEndPoint LocalEndpoint,
    IPEndPoint? HostEndpoint,
    IEnumerable<ControllerUser>? InitialPeers = null,
    Action<IUserNotificationHandler>? RegisterHandler = null,
    Action<IUserNotificationHandler>? UnregisterHandler = null);
