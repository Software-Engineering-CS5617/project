// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR;

namespace Cloud.Networking.Server.Models;

/// <summary>
/// A hub for broadcasting and syncing messages.
/// </summary>
public class MessageHub : Hub
{
    /// <summary>Identifier used for messages originating from the server.</summary>
    public const string ServerSenderId = "server";

    /// <summary>The longest message content (after trimming) that is accepted.</summary>
    public const int MaxContentLength = 2_000;

    private readonly IMessageStore _store;

    /// <summary>
    /// Initializes the hub with <see cref="IMessageStore"/> instance.
    /// </summary>
    /// <param name="store">a <see cref="IMessageStore"/> instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public MessageHub(IMessageStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
    }

    /// <summary>
    /// Broadcasts messages to all clients connected through this hub
    /// </summary>
    /// <param name="content">the content of the message to be sent.</param>
    /// <returns>the <see cref="Message"/> that was sent</returns>
    /// <exception cref="HubException">Thrown when the <paramref name="content"/> is empty, whitespace or longer than <see cref="MaxContentLength"/>.</exception>
    public async Task<Message> SendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new HubException("Message cannot be empty.");
        }

        string trimmed = content.Trim();
        if (trimmed.Length > MaxContentLength)
        {
            throw new HubException($"Message cannot be longer than {MaxContentLength} characters.");
        }

        Message message = new() {
            SenderId = Context.ConnectionId, // TODO: some other id for auth and better identity
            Content = trimmed
        };

        await _store.AddAsync(message);
        await Clients.All.SendAsync(Contract.ReceiveMessage, message);
        return message;
    }

    /// <summary>
    /// Gets the messages created after <paramref name="since"/> in <see cref="IMessageStore"/> instance.
    /// </summary>
    /// <param name="since">the timestamp which determines the threshold.</param>
    /// <returns>A list of messages after "since".</returns>
    public async Task<Message[]> GetMessagesSince(DateTimeOffset since)
    {
        return [.. await _store.GetSinceAsync(since)];
    }
}
