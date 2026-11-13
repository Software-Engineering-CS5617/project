namespace Incidents.Test.Implementations;

using Cloud.Persistence;

public class IncidentCloudStorageService : IStorage
{
    private readonly Dictionary<string, byte[]> _memory = new();

    public Task<bool> Create(string key, byte[] data)
    {
        _memory[key] = data;
        return Task.FromResult(true);
    }

    public Task<(bool Success, byte[] Data)> Read(string key)
    {
        if (_memory.ContainsKey(key))
        {
            return Task.FromResult((true, _memory[key]));
        }

        return Task.FromResult((false, Array.Empty<byte>()));
    }

    public Task<(bool Success, string Etag)> ReadEtag(string key)
    {
        if (_memory.ContainsKey(key))
        {
            return Task.FromResult((true, Guid.NewGuid().ToString()));
        }

        return Task.FromResult((false, string.Empty));
    }

    public Task<(bool Success, Dictionary<string, string> Etags)> ReadEtags(string[] keys)
    {
        Dictionary<string, string> etags = new();
        bool success = true;

        foreach (string key in keys)
        {
            if (_memory.ContainsKey(key))
            {
                etags[key] = Guid.NewGuid().ToString();
            }
            else
            {
                success = false;
            }
        }

        return Task.FromResult((success, etags));
    }

    public Task<(bool Success, Dictionary<string, string>)> ReadAllEtags()
    {
        Dictionary<string, string> etags = new();

        foreach (KeyValuePair<string, byte[]> kvp in _memory)
        {
            etags[kvp.Key] = Guid.NewGuid().ToString();
        }

        return Task.FromResult((true, etags));
    }

    public Task<bool> Update(string key, byte[] data, string etag)
    {
        if (_memory.ContainsKey(key))
        {
            _memory[key] = data;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> Delete(string key)
    {
        if (_memory.ContainsKey(key))
        {
            _memory.Remove(key);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> CreateAll(Dictionary<string, byte[]> items)
    {
        foreach ((string key, byte[] value) in items)
        {
            _memory[key] = value;
        }

        return Task.FromResult(true);
    }

    public async Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
    {
        Dictionary<string, byte[]> temp = new();

        foreach (string key in keys)
        {
            (bool success, byte[] data) = await Read(key);

            if (!success)
            {
                return (false, new Dictionary<string, byte[]>());
            }

            temp[key] = data;
        }

        return (true, temp);
    }

    public Task<(bool Success, List<string>)> GetAllKeys()
    {
        return Task.FromResult((true, _memory.Keys.ToList()));
    }

    public async Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags)
    {
        foreach ((string key, byte[] value) in items)
        {
            if (!_memory.ContainsKey(key))
            {
                return false;
            }
        }

        foreach ((string key, byte[] value) in items)
        {
            await Update(key, value, "etag");
        }

        return true;
    }

    public async Task<bool> DeleteAll(string[] keys)
    {
        foreach (string key in keys)
        {
            if (!_memory.ContainsKey(key))
            {
                return false;
            }
        }

        foreach (string key in keys)
        {
            await Delete(key);
        }

        return true;
    }

    public Task<(bool Success, Dictionary<string, byte[]> Items)> ReadLatest(int n)
    {
        Dictionary<string, byte[]> latestItems = _memory
            .TakeLast(n)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return Task.FromResult((true, latestItems));
    }
}
