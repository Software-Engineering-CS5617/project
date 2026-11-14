// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// An interface for a store of messages.
/// </summary>
public interface IMessageStore
{
    /// <summary>Raised after a message has been stored</summary>
    event Action<Message>? MessageAdded;

    /// <summary>
    /// Adds a message in the store.
    /// </summary>
    /// <param name="message">The message to store</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(Message message);

    /// <summary>
    /// Gets the stored messages created after <paramref name="since"/>.
    /// </summary>
    /// <param name="since">Only messages with a later timestamp are returned</param>
    /// <returns>A read-only list of messages, oldest first</returns>
    Task<IReadOnlyList<Message>> GetSinceAsync(DateTimeOffset since);

    /// <summary>
    /// Gets the most recently stored messages.
    /// </summary>
    /// <param name="count">The maximum number of messages to return</param>
    /// <returns>A read-only list of at most <paramref name="count"/> messages, newest first</returns>
    Task<IReadOnlyList<Message>> GetRecentAsync(int count);
}
