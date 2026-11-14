using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cloud.Persistence;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Services;

public sealed class WhiteboardSnapshotStore
{
    private readonly IStorage _storage;

    public WhiteboardSnapshotStore(IStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
    }

    public static async Task<WhiteboardSnapshotStore> CreateAsync()
    {
        IStorage storage = await StorageFactory.CreateAsync(ModuleType.WhiteBoard);
        return new WhiteboardSnapshotStore(storage);
    }

    private static string GetKeyPrefix(string sessionId)
    {
        return $"session:{sessionId}:snapshot:";
    }

    private static string GetSnapshotKey(string sessionId, string snapshotId)
    {
        return $"{GetKeyPrefix(sessionId)}{snapshotId}";
    }

    public async Task<WhiteboardSnapshotSummary> SaveSnapshotAsync(string sessionId, string userId, IEnumerable<ItemDto> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(items);

       var snapshot = new WhiteboardSnapshot
       {
            SessionId = sessionId,
            SnapshotId = Guid.NewGuid().ToString("N"),
            SavedBy = userId,
            SavedAt = DateTimeOffset.UtcNow,
            Items = items.ToList()
       };

       byte[] data = JsonSerializer.SerializeToUtf8Bytes(snapshot);
       string key = GetSnapshotKey(sessionId, snapshot.SnapshotId);

       if(!await _storage.Create(key, data))
       {
            throw new InvalidOperationException("Cloud failed to save whiteboard snapshot");
       }

       return new WhiteboardSnapshotSummary
       {
            SnapshotId = snapshot.SnapshotId,
            SavedBy = snapshot.SavedBy,
            SavedAt = snapshot.SavedAt
       };
    }

    public async Task<List<WhiteboardSnapshotSummary>> ListAsync(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var (success, keys) = await _storage.GetAllKeys();

        if(!success)
        {
            throw new InvalidOperationException("Cloud failed to list whiteboard snapshots");
        }

        string prefix = GetKeyPrefix(sessionId);
        var snapshots = new List<WhiteboardSnapshotSummary>();

        foreach(string key in keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
        {
            var (readSuccess, data) = await _storage.Read(key);

            if(!readSuccess)
            {
                continue;
            }

            WhiteboardSnapshot? snapshot = JsonSerializer.Deserialize<WhiteboardSnapshot>(data);

            if(snapshot is null || snapshot.SessionId != sessionId)
            {
                continue;
            }

            snapshots.Add(new WhiteboardSnapshotSummary
            {
                SnapshotId = snapshot.SnapshotId,
                SavedBy = snapshot.SavedBy,
                SavedAt = snapshot.SavedAt
            });
        }

        return snapshots.OrderByDescending(snapshot => snapshot.SavedAt).ToList();
    }

    public async Task<WhiteboardSnapshot> LoadAsync(string sessionId, string snapshotId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);

        var (success, data) = await _storage.Read(GetSnapshotKey(sessionId, snapshotId));

        if (!success)
        {
            throw new InvalidOperationException("Could not load the selected snapshot from Cloud");
        }

        WhiteboardSnapshot? snapshot = JsonSerializer.Deserialize<WhiteboardSnapshot>(data);

        if(snapshot is null || snapshot.SessionId != sessionId || snapshot.SnapshotId != snapshotId)
        {
            throw new InvalidOperationException("The loaded snapshot is invalid");
        }

        return snapshot;
    }
}