using Microsoft.Azure.Cosmos;
using Persistence;

public static class StorageFactory
{
    // A single instance of CosmosClient is created and reused for all storage instances.
    private static readonly CosmosClient _cosmosClient =
        new CosmosClient(
            CosmosSettings.Endpoint,
            CosmosSettings.Key);

    // This method asynchronously creates an IStorage instance for the specified module type.
    public static async Task<IStorage> CreateAsync(ModuleType module)
    {
        string containerName = GetContainerName(module);

        CosmosStorage storage = new CosmosStorage(
            _cosmosClient,
            CosmosSettings.DatabaseName,
            containerName);

        await storage.InitializeAsync();

        return storage;
    }

    private static string GetContainerName(ModuleType module)
    {
        return module switch
        {
            ModuleType.Chat => "Chat",
            ModuleType.WhiteBoard => "Whiteboard",
            ModuleType.Networking => "Networking",
            ModuleType.FileSynchronizer => "FileSynchronizer",
            // will add more modules here
            _ => throw new ArgumentException($"Unsupported module: {module}")
        };
    }
}