using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Cloud.Persistence;
/// <summary>
/// Implements IStorage using Azure Cosmos DB
/// </summary>
internal class CosmosStorage : IStorage
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _containerName;
    private Container _container = null!;

    /// <summary>
    /// Initialises a new instance of CosmosStorage class
    /// </summary>
    /// <param name="client"></param>
    /// <param name="databaseName"></param>
    /// <param name="containerName"></param>
    public CosmosStorage(
        CosmosClient client,
        string databaseName,
        string containerName)
    {
        _client = client;
        _databaseName = databaseName;
        _containerName = containerName;
    }

    /// <summary>
    /// This method initializes the Cosmos DB container for the storage instance.
    /// Call this method before doing the first storage operation
    /// </summary>
    /// <returns>Task</returns>
    internal async Task InitializeAsync()
    {
        Database database =
            await _client.CreateDatabaseIfNotExistsAsync(
                _databaseName,
                ThroughputProperties.CreateManualThroughput(1000));

        ContainerResponse response =
            await database.CreateContainerIfNotExistsAsync(
                _containerName,
                "/id");

        _container = response.Container;

    }

    // All CRUD operations are done asynchronously.
    // IStorage interface methods are implemented here.


    /// <inheritdoc/>
    public async Task<bool> Create(string key, byte[] data)
    {
        PersistenceItem item = new PersistenceItem {
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    /* Concurrent updates are handled using _etag (version stamp) of the Cosmos DB item
       Each write changes the ETag
       For updating a key-value pair, one has to read the current ETag of the item */

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

    /// <inheritdoc/>

    /* One has to correctly match the ETag he read earlier with the current ETag of the item
       Only one of the concurrent writers who read the same _etag succeeds in Update operation
       This is because the winner changes the ETag by writing which makes others' ETags stale */
    public async Task<bool> Update(string key, byte[] data, string etag)
    {

        PersistenceItem item = new PersistenceItem {
            Id = key,
            Data = data
        };

        try
        {
            await _container.ReplaceItemAsync(
                item,
                key,
                new PartitionKey(key),
                new ItemRequestOptions {
                    IfMatchEtag = etag
                });

            return true;

        }
        catch (CosmosException ex)
        {
            // The ETag does not match, so the item has been modified by another process
            if (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                Console.WriteLine($"Update failed for key '{key}': ETag mismatch");
            }
            return false;
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<bool> CreateAll(Dictionary<string, byte[]> items)
    {
        foreach (KeyValuePair<string, byte[]> item in items)
        {
            if (!await Create(item.Key, item.Value))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc/>
    // Empty array is returned even if one read fails.
    // All other bulk CRUD operations are atomic like this
    public async Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
    {
        Dictionary<string, byte[]> result = [];
        foreach (string key in keys)
        {
            (bool success, byte[] data) = await Read(key);
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

    /// <inheritdoc/>
    public async Task<(bool Success, List<string>)> GetAllKeys()
    {
        List<string> keys = [];

        try
        {
            using FeedIterator<string> iterator =
                _container.GetItemLinqQueryable<PersistenceItem>() //LINQ helps us to query using C# avoiding SQL
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

    /// <inheritdoc/>

    /*
     The user calls the UpdateAll method only after getting all the Etags from the module's container.
     A dictionary of Id : Etag of each item is returned
    */

    public async Task<(bool Success, Dictionary<string, string>)> ReadAllEtags()
    {
        Dictionary<string, string> etags = [];

        try
        {
            using FeedIterator<PersistenceItem> iterator =
                _container.GetItemLinqQueryable<PersistenceItem>()
                    .ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                FeedResponse<PersistenceItem> page = await iterator.ReadNextAsync();

                foreach (PersistenceItem item in page)
                {
                    string? etag = item.Etag;

                    if (etag is null)
                    {
                        return (false, new Dictionary<string, string>());
                    }
                    else
                    {
                        etags[item.Id] = item.Etag;
                    }
                }
            }

            return (true, etags);
        }
        catch (CosmosException)
        {
            return (false, new Dictionary<string, string>());
        }
    }

    /// <inheritdoc/>
    /* 
     Call UpdateAll method with the key-value pairs to be updated and Etags of all items from the 
     container. 
     The method will use only the etags of the required items from the Etags of all items
    */
    public async Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags)
    {
        try
        {
            foreach (KeyValuePair<string, byte[]> item in items)
            {
                bool success = await Update(
                    item.Key,
                    item.Value,
                    etags[item.Key]);

                if (!success)
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

    /// <inheritdoc/>
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
