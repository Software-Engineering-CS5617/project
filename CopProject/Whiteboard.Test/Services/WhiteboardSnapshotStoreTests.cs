/// Author : Karthik Krishna M
/// Roll Number : 112301014
/// LICENSE : MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud.Persistence;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;
using Whiteboard.Services;
using Xunit;

namespace Whiteboard.Test.Services;

public class WhiteboardSnapshotStoreTests
{
    [Fact]
    public async Task SaveSnapshotAsync_StoresSnapshot()
    {
        var storage = new InMemoryStorage();
        var store = new WhiteboardSnapshotStore(storage);
        var items = new List<ItemDto>();

        WhiteboardSnapshotSummary summary = await store.SaveSnapshotAsync("session-1", "user-1", items);

        var snapshot = await store.LoadAsync("session-1", summary.SnapshotId);

        Assert.Equal("session-1", snapshot.SessionId);
        Assert.Equal(summary.SnapshotId, snapshot.SnapshotId);
        Assert.Equal("user-1", snapshot.SavedBy);
        Assert.Empty(snapshot.Items);
        Assert.NotEqual(default, snapshot.SavedAt);
    }

    [Fact]
    public async Task ListAsync_ReturnSessionSnapshots()
    {
        var storage = new InMemoryStorage();
        var store = new WhiteboardSnapshotStore(storage);

        WhiteboardSnapshotSummary first = await store.SaveSnapshotAsync("session-1", "user-1", Array.Empty<ItemDto>());
        await Task.Delay(10);
        WhiteboardSnapshotSummary second = await store.SaveSnapshotAsync("session-1", "user-2", Array.Empty<ItemDto>());
        await store.SaveSnapshotAsync("session-2", "user-3", Array.Empty<ItemDto>());

        List<WhiteboardSnapshotSummary> snapshots = await store.ListAsync("session-1");

        Assert.Equal(2, snapshots.Count);
        Assert.Equal(second.SnapshotId, snapshots[0].SnapshotId);
        Assert.Equal(first.SnapshotId, snapshots[1].SnapshotId);
    }

    [Fact]
    public async Task SaveSnapshotAsync_StorageCreateFail()
    {
        var storage = new InMemoryStorage { FailCreate = true };
        var store = new WhiteboardSnapshotStore(storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveSnapshotAsync("session-1", "user-1", Array.Empty<ItemDto>()));
    }

    [Fact]
    public async Task LoadAsync_SnapshotDoesNotExist()
    {
        var store = new WhiteboardSnapshotStore(new InMemoryStorage());

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync("session-1", "missing-snapshot"));
    }

    [Fact]
    public async Task ListAsync_StorageCannotListKeys()
    {
        var storage = new InMemoryStorage { FailGetAllKeys = true };
        var store = new WhiteboardSnapshotStore(storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ListAsync("session-1"));
    }

    private sealed class InMemoryStorage : IStorage
    {
        private readonly Dictionary<string, byte[]> _items = new();

        public bool FailCreate { get; set; }
        public bool FailGetAllKeys { get; set; }

        public Task<bool> Create(string key, byte[] data)
        {
            if (FailCreate || _items.ContainsKey(key))
            {
                return Task.FromResult(false);
            }

            _items[key] = data;
            return Task.FromResult(true);
        }

        public Task<(bool Success, byte[] Data)> Read(string key)
        {
            return Task.FromResult(_items.TryGetValue(key, out byte[]? data)
                ? (true, data!)
                : (false, Array.Empty<byte>()));
        }

        public Task<(bool Success, string Etag)> ReadEtag(string key) => Task.FromResult((false, string.Empty));

        public Task<bool> Update(string key, byte[] data, string etag) => Task.FromResult(false);

        public Task<bool> Delete(string key) => Task.FromResult(_items.Remove(key));

        public Task<bool> CreateAll(Dictionary<string, byte[]> items) => Task.FromResult(false);

        public Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
        {
            Dictionary<string, byte[]> found = _items.Where(entry => keys.Contains(entry.Key)).ToDictionary(entry => entry.Key, entry => entry.Value);

            return Task.FromResult((true, found));
        }

        public Task<(bool Success, List<string>)> GetAllKeys() =>
            Task.FromResult(FailGetAllKeys
                ? (false, new List<string>())
                : (true, _items.Keys.ToList()));

        public Task<(bool Success, Dictionary<string, string>)> ReadAllEtags() => Task.FromResult((false, new Dictionary<string, string>()));

        public Task<(bool Success, Dictionary<string, string> Etags)> ReadEtags(string[] keys) => Task.FromResult((false, new Dictionary<string, string>()));

        public Task<bool> UpdateAll( Dictionary<string, byte[]> items, Dictionary<string, string> etags) => Task.FromResult(false);

        public Task<bool> DeleteAll(string[] keys) => Task.FromResult(false);

        public Task<(bool Success, Dictionary<string, byte[]> Items)> ReadLatest(int n) => Task.FromResult((true, new Dictionary<string, byte[]>()));
    }
}
