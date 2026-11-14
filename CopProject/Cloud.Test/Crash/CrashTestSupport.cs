using System.Diagnostics;
using Cloud.Crash;
using Cloud.Persistence;

namespace Cloud.Test.Crash;

internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "crash-test-" + Guid.NewGuid().ToString("N"));
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}

internal sealed class FakeUploader : ICrashUploader
{
    public Func<Task<bool>> Behavior { get; set; } = () => Task.FromResult(true);

    public List<byte[]> Uploaded { get; } = [];

    public Task<bool> UploadAsync(Guid reportId, byte[] reportXml, CancellationToken cancellationToken)
    {
        Trace.TraceInformation($"FakeUploader.UploadAsync: received report {reportId}.");
        lock (Uploaded)
        {
            Uploaded.Add(reportXml);
        }

        return Behavior();
    }
}

internal sealed class InMemoryStorage : IStorage
{
    private readonly Dictionary<string, (byte[] Data, int Version)> _items = [];

    public int FailNextUpdates { get; set; }

    public bool FailGetAllKeys { get; set; }

    public Action<string>? BeforeCreate { get; set; }

    public Task<bool> Create(string key, byte[] data)
    {
        Trace.TraceInformation($"InMemoryStorage.Create: creating key '{key}'.");
        BeforeCreate?.Invoke(key);

        if (_items.ContainsKey(key))
        {
            Trace.TraceWarning($"InMemoryStorage.Create: key '{key}' already exists.");
            return Task.FromResult(false);
        }

        _items[key] = (data, 1);
        return Task.FromResult(true);
    }

    public Task<(bool Success, byte[] Data)> Read(string key)
    {
        Trace.TraceInformation($"InMemoryStorage.Read: reading key '{key}'.");
        return Task.FromResult(_items.TryGetValue(key, out (byte[] Data, int Version) item) ? (true, item.Data) : (false, []));
    }

    public Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
    {
        return Task.FromResult((true, _items.Where(i => keys.Contains(i.Key)).ToDictionary(i => i.Key, i => i.Value.Data)));
    }

    public Task<bool> Update(string key, byte[] data, string etag)
    {
        Trace.TraceInformation($"InMemoryStorage.Update: updating key '{key}'.");
        if (FailNextUpdates > 0)
        {
            FailNextUpdates--;
            Trace.TraceWarning($"InMemoryStorage.Update: simulated failure for key '{key}'.");
            return Task.FromResult(false);
        }

        if (!_items.TryGetValue(key, out (byte[] Data, int Version) item) || item.Version.ToString() != etag)
        {
            Trace.TraceWarning($"InMemoryStorage.Update: ETag conflict for key '{key}'.");
            return Task.FromResult(false);
        }

        _items[key] = (data, item.Version + 1);
        return Task.FromResult(true);
    }

    public Task<bool> Delete(string key)
    {
        return Task.FromResult(_items.Remove(key));
    }

    public Task<(bool Success, List<string>)> GetAllKeys()
    {
        Trace.TraceInformation("InMemoryStorage.GetAllKeys: listing keys.");
        return Task.FromResult((!FailGetAllKeys, _items.Keys.ToList()));
    }

    public Task<(bool Success, string Etag)> ReadEtag(string key)
    {
        Trace.TraceInformation($"InMemoryStorage.ReadEtag: reading ETag for key '{key}'.");
        return Task.FromResult(_items.TryGetValue(key, out (byte[] Data, int Version) item) ? (true, item.Version.ToString()) : (false, string.Empty));
    }

    public Task<bool> CreateAll(Dictionary<string, byte[]> items)
    {
        if (items.Keys.Any(_items.ContainsKey))
        {
            return Task.FromResult(false);
        }

        foreach ((string key, byte[] data) in items)
        {
            _items[key] = (data, 1);
        }

        return Task.FromResult(true);
    }

    public Task<(bool Success, Dictionary<string, string>)> ReadAllEtags()
    {
        return Task.FromResult((true, _items.ToDictionary(i => i.Key, i => i.Value.Version.ToString())));
    }

    public Task<(bool Success, Dictionary<string, string> Etags)> ReadEtags(string[] keys)
    {
        return Task.FromResult((true, _items.Where(i => keys.Contains(i.Key)).ToDictionary(i => i.Key, i => i.Value.Version.ToString())));
    }

    public Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags)
    {
        foreach (string key in items.Keys)
        {
            if (!_items.TryGetValue(key, out (byte[] Data, int Version) item) || !etags.TryGetValue(key, out string? etag) || item.Version.ToString() != etag)
            {
                return Task.FromResult(false);
            }
        }

        foreach ((string key, byte[] data) in items)
        {
            _items[key] = (data, _items[key].Version + 1);
        }

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAll(string[] keys)
    {
        foreach (string key in keys)
        {
            _items.Remove(key);
        }

        return Task.FromResult(true);
    }

    public Task<(bool Success, Dictionary<string, byte[]> Items)> ReadLatest(int n)
    {
        return Task.FromResult((true, _items.TakeLast(n).ToDictionary(i => i.Key, i => i.Value.Data)));
    }

    public Task Put(string key, byte[] data)
    {
        _items[key] = (data, 1);
        return Task.CompletedTask;
    }
}

internal sealed class FakeCrashReportService : ICrashReportService
{
    public List<CrashGroup> Groups { get; set; } = [];

    public List<CrashReport> Reports { get; set; } = [];

    public List<CrashReport> PendingReports { get; set; } = [];

    public bool Fail { get; set; }

    public int Pending { get; set; }

    public int RetryResult { get; set; }

    public Task<IReadOnlyList<CrashGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        Trace.TraceInformation("FakeCrashReportService.GetGroupsAsync: returning fake groups.");
        return Fail ? throw new InvalidOperationException("down") : Task.FromResult<IReadOnlyList<CrashGroup>>(Groups);
    }

    public Task<IReadOnlyList<CrashReport>> GetReportsAsync(CrashGroup group, CancellationToken cancellationToken = default)
    {
        Trace.TraceInformation("FakeCrashReportService.GetReportsAsync: returning fake reports.");
        return Fail ? throw new InvalidOperationException("down") : Task.FromResult<IReadOnlyList<CrashReport>>(Reports);
    }

    public IReadOnlyList<CrashReport> GetPendingReports()
    {
        return PendingReports;
    }

    public int GetPendingCount()
    {
        return Pending;
    }

    public Task<int> RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        Trace.TraceInformation("FakeCrashReportService.RetryPendingAsync: returning fake retry result.");
        return Fail ? throw new InvalidOperationException("down") : Task.FromResult(RetryResult);
    }
}

internal static class Samples
{
    public static CrashReport Report(string type = "System.InvalidOperationException", string method = "Run", string assembly = "Chat", DateTime? when = null)
    {
        return new CrashReport {
            TimestampUtc = when ?? DateTime.UtcNow,
            Exceptions = [
                new CrashExceptionInfo {
                    Type = type,
                    Message = "boom",
                    Frames = [new CrashFrame { DeclaringType = assembly + ".A", Method = method, Assembly = assembly }]
                }
            ]
        };
    }

    public static CrashGroup Group(CrashReport report)
    {
        return CrashGroup.FromReport(CrashFingerprinter.Analyze(report), report);
    }
}
