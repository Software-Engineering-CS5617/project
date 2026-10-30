// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR;

namespace Cloud.Networking.Server;

/// <summary>
/// SignalR hub that receives messages from clients and broadcasts them.
/// </summary>
public class MessageHub : Hub
{
    /// <summary>
    /// Doing RPC i.e server calling RecieveMessage on the clients.
    /// </summary>
    public async Task SendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new HubException("Message cannot be empty.");
        }

        Message message = new() {
            SenderId = Context.ConnectionId, // TODO: some other id for auth (MAYBE)
            Content = content.Trim()
        };

        await Clients.All.SendAsync(Contract.ReceiveMessage, message);
    }
}
