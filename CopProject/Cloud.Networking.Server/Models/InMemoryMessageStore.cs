// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Stores the message in memory as a FIFO queue and MaxMessages limit.
/// </summary>
public sealed class InMemoryMessageStore : IMessageStore
{
    public const int DefaultMaxMessages = 10_000;

    public int MaxMessages { get; }
    private readonly object _gate = new();
    private readonly Queue<Message> _messages = new();

    /// <summary>
    /// Initializes the store with <paramref name="maxMessages"/> limit.
    /// </summary>
    /// <param name="maxMessages">A maximum number of messages limit</param>
    public InMemoryMessageStore(int maxMessages = DefaultMaxMessages)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessages);
        MaxMessages = maxMessages;
    }

    /// <inheritdoc />
    public event Action<Message>? MessageAdded;

    /// <inheritdoc />
    public Task AddAsync(Message message)
    {
        // FIFO Policy
        ArgumentNullException.ThrowIfNull(message);

        lock (_gate)
        {
            _messages.Enqueue(message);
            while (_messages.Count > MaxMessages)
            {
                _messages.Dequeue();
            }
        }

        // subscribers could be slow (hence outside) 
        MessageAdded?.Invoke(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Message>> GetSinceAsync(DateTimeOffset since)
    {
        Message[] newer;
        lock (_gate)
        {
            newer = _messages.Where(m => m.Timestamp > since).ToArray();
        }

        // expensive sorting (hence outside)
        IReadOnlyList<Message> result = newer.OrderBy(m => m.Timestamp).ToArray();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Message>> GetRecentAsync(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        lock (_gate)
        {
            // as we need newest first
            IReadOnlyList<Message> result = _messages
                .Skip(Math.Max(0, _messages.Count - count))
                .Reverse()
                .ToArray();

            return Task.FromResult(result);
        }
    }
}
