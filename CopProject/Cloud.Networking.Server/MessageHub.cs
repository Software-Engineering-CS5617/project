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
    /// Server sends messages by calling "RecieveMessage" on the clients.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    /// <returns>The message created by the server</returns>
    /// <exception cref="HubException">Thrown when the message content is null or whitespace.</exception>
    public async Task<Message> SendMessage(string content)
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
        return message;
    }
}
