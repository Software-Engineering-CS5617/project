# Progress : Persistence Module

## Azure Cosmos DB
- We store key-value pairs in Cosmos DB. <br> In Cosmos DB, `id` denotes the key and `data` denotes the value. There are also some metadata fields for each item.<br><br>
We recieve `id` : `string` and `data` : `byte[]` from the users. <br>Cosmos DB serializes `data` to a `Base64String` and stores it in a JSON document 

- Our Cosmos DB database has a **shared database-level throughput of 1000 RU/s**. <br>
*We chose shared database-level throughput over dedicated container-level throughput because in the latter design, some throughput is provisioned for each container even if it is idle.*
- Each module gets a container of its own. 
- We use this free-tier Cosmos DB services using Azure Students Subscription
- In Cosmos DB, an item cannot be larger than 2MB, so we plan to store larger files in `Azure Blob Storage` and store their reference in Cosmos DB

## Azure Cosmos DB SDK
- We make use of the Azure Cosmos DB .NET SDK to implement storage operations
- This is like a higher-level wrapper around the Cosmos DB REST API
- We chose this because it has built-in serialization support and ETag concurrency control support
- We create only one `CosmosClient` for all storage instances

## StorageFactory
- We use the Factory pattern to create a new storage instance and initialize the container corresponding to the module
```
IStorage storage =
        await StorageFactory.CreateAsync(ModuleType.LocationTracker);
```


## IStorage
`IStorage` is the public interface for storage operations. <br>We designed the interface after taking suggestions from the Incidents team.

The interface supports the following
- Basic CRUD operations
- Handling Concurrent updates using `ETags` [see the next section](#concurrency-control-using-etags)
- Bulk CRUD operations

`IStorage` has the following methods:
- `Create(key, data)` : Creates and stores a key-value pair
- `Read(key)` : Read the data corresponding to the key
- `ReadEtag(key)` : Read the `_etag` of the item corresponding to the key
- `Update(key, data, etag)` : Updates the data corresponding to the key, provided the ETag matches
- `Delete(key)` : Deletes the item
- `CreateAll({key, data})` : Creates and stores all key-value pairs in the dictionary 
- `ReadAll([key])` : Returns the data corresponding to each key
- `GetAllKeys()` : Returns all the keys in the container of this module
- `ReadAllEtags()` : Returns ETags of all the items in the container
- `UpdateAll({key, data}, )` : Updates all key-value pairs given in the dictionary
- `DeleteAll([key])` : Deletes the items corresponding to the given keys 

All operations are asynchronous<br>
All the methods return a boolean flag to show success or failure

```
public interface IStorage
{
    
    Task<bool> Create(string key, byte[] data);

    Task<(bool Success, byte[] Data)> Read(string key);

    Task<(bool Success, string Etag)> ReadEtag(string key);

    Task<bool> Update(string key, byte[] data, string etag);

    Task<bool> Delete(string key);

    Task<bool> CreateAll(Dictionary<string, byte[]> items);

    Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys);

    Task<(bool Success, List<string>)> GetAllKeys();

    Task<(bool Success, Dictionary<string, string>)> ReadAllEtags();
    
    Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags);
   
    Task<bool> DeleteAll(string[] keys);

}

```

## Concurrency Control using ETags
This is very similar to the hashing approach we discussed in the meeting ([Microsoft-Learn/Optimistic-Concurrency-Control](https://learn.microsoft.com/en-us/azure/cosmos-db/database-transactions-optimistic-concurrency))
- `_etag` acts as a version stamp for a Cosmos DB item
- It changes when we write or update the item


### When two or more users attempt to update the same item simultaneously : 

- **Users who want to update an item should read the `_etag` of the item using `ReadEtag(key)` method. <br>All of them will read the same ETag.** 
- **Now all of them call `Update(key, updatedData, etag).`<br>We compare the ETag provided by the writer with the ETag of the item in the cloud and the update succeeds only if the ETags match.**
- **Only one of the writers succeeds in updating the item and thus changes the ETag of the item. <br>Thus all others' Etags become stale and they fail.**

- So, only one of the concurrent updates succeeds and the others fail





