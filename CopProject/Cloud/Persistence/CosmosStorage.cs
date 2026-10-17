using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Persistence;

public class CosmosStorage : IStorage
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _containerName;
    private Container _container = null!;

    public CosmosStorage(
        CosmosClient client,
        string databaseName,
        string containerName)
    {
        _client = client;
        _databaseName = databaseName;
        _containerName = containerName;
    }

    // This method initializes the Cosmos DB container for the storage instance.
    public async Task InitializeAsync()
    {
        Database database =
            await _client.CreateDatabaseIfNotExistsAsync(_databaseName);

        ContainerResponse response =
            await database.CreateContainerIfNotExistsAsync(
                _containerName,
                "/id");

        _container = response.Container;

    }

    // All CRUD operations are done asynchronously.
    // IStorage interface methods are implemented here.
    public async Task<bool> Create(string key, byte[] data)
    {
        PersistenceItem item = new PersistenceItem
        {
            Id = key,
            Data = data
        };

        try
        {
            await _container.CreateItemAsync(item, new PartitionKey(item.Id));
            return true;
        }

        catch (CosmosException)
        {
            return false;
        }
    }

    public async Task<(bool Success, byte[] Data)> Read(string key)
    {
        try
        {
            ItemResponse<PersistenceItem> response =
                await _container.ReadItemAsync<PersistenceItem>(
                    key,
                    new PartitionKey(key));

            return (true, response.Resource.Data);
        }
        catch (CosmosException)
        {
            return (false, Array.Empty<byte>());
        }
    }


    public async Task<(bool Success, string Etag)> ReadEtag(string key)
    {
        try
        {
            ItemResponse<PersistenceItem> response =
                await _container.ReadItemAsync<PersistenceItem>(
                    key,
                    new PartitionKey(key));
            return (true, response.ETag);
        }
        catch (CosmosException)
        {
            return (false, string.Empty);
        }
    }

    public async Task<bool> Update(string key, byte[] data, string etag)
    {

        PersistenceItem item = new PersistenceItem
        {
            Id = key,
            Data = data
        };

        try
        {
            await _container.ReplaceItemAsync(
                item,
                key,
                new PartitionKey(key),
                new ItemRequestOptions
                {
                    IfMatchEtag = etag
                });

            return true;

        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            // The ETag does not match, so the item has been modified by another process
            Console.WriteLine($"Update failed for key '{key}': ETag mismatch");
            return false;
        }
    }

    public async Task<bool> Delete(string key)
    {
        try
        {
            await _container.DeleteItemAsync<PersistenceItem>(
                key,
                new PartitionKey(key));
            return true;
        }
        catch (CosmosException)
        {
            return false;
        }
    }


    public async Task<bool> CreateAll(Dictionary<string, byte[]> items)
    {
        foreach (var item in items)
        {
            if (!await Create(item.Key, item.Value))
            {
                return false;
            }
        }
        return true;
    }

    public async Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
    {
        Dictionary<string, byte[]> result = new Dictionary<string, byte[]>();
        foreach (string key in keys)
        {
            var (success, data) = await Read(key);
            if (success)
            {
                result[key] = data;
            }
            else
            {
                return (false, new Dictionary<string, byte[]>());
            }
        }
        return (true, result);
    }


    public async Task<(bool Success, List<string>)> GetAllKeys()
    {
        List<string> keys = new List<string>();

        try
        {
            using FeedIterator<string> iterator =
                _container.GetItemLinqQueryable<PersistenceItem>()
                          .Select(item => item.Id)
                          .ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                FeedResponse<string> page = await iterator.ReadNextAsync();
                keys.AddRange(page);
            }

            return (true, keys);
        }
        catch (CosmosException)
        {
            return (false, new List<string>());
        }
    }


    public async Task<bool> UpdateAll(Dictionary<string, byte[]> items)
    {
        try
        {
            foreach (var item in items)
            {
                var result = await ReadEtag(item.Key);
                if (!result.Success)
                {
                    return false;
                }
                bool updateSuccess = await Update(item.Key, item.Value, result.Etag);
                if (!updateSuccess)
                {
                    return false;
                }
            }

            return true;

        }
        catch (CosmosException)
        {
            return false;
        }
    }


    public async Task<bool> DeleteAll(string[] keys)
    {
        foreach (string key in keys)
        {
            if (!await Delete(key))
            {
                return false;
            }
        }
        return true;
    }




}
