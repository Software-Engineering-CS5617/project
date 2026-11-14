// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking;

/// <summary>
/// Method names strings shared by the client and the server.
/// </summary>
public static class Contract
{
    /// <summary>
    /// Method name string for the server-side method invoked by clients (to broadcast to all connected clients).
    /// </summary>
    public const string SendMessage = "SendMessage";

    /// <summary>
    /// Method name string for the client-side method invoked by the server (to receive it)
    /// </summary>
    public const string ReceiveMessage = "ReceiveMessage";

    /// <summary>
    /// Method name string for the server-side method that returns the messages created after a given time.
    /// Used by Sync.
    /// </summary>
    public const string GetMessagesSince = "GetMessagesSince";
}
