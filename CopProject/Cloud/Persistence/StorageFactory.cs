using Cloud.Persistence;
using Microsoft.Azure.Cosmos;

/// <summary>
/// Creates IStorage instances for application modules
/// All modules share a Cosmosclient and each module gets its own container
/// </summary>
public static class StorageFactory
{
    // A single instance of CosmosClient is created and reused for all storage instances.
    private static readonly CosmosClient s_cosmosClient =
        new(CosmosSettings.Endpoint, CosmosSettings.Key);

    /// <summary>
    /// This method asynchronously creates an IStorage instance for the specified module type if it doesn't exist .
    /// </summary>
    /// <param name="module"></param>
    /// <returns>IStorage instance ready to use</returns>
    public static async Task<IStorage> CreateAsync(ModuleType module)
    {
        string containerName = GetContainerName(module);

        CosmosStorage storage = new CosmosStorage(
            s_cosmosClient,
            CosmosSettings.DatabaseName,
            containerName);

        await storage.InitializeAsync();

        return storage;
    }

    private static string GetContainerName(ModuleType module)
    {
        return module switch {
            ModuleType.Chat => "Chat",
            ModuleType.WhiteBoard => "Whiteboard",
            ModuleType.Networking => "Networking",
            ModuleType.FileSynchronizer => "FileSynchronizer",
            ModuleType.IncidentManagement => "IncidentManagement",
            ModuleType.Controller => "Controller",
            ModuleType.Insights => "Insights",
            ModuleType.LocationTracker => "LocationTracker",
            ModuleType.UX => "UX",
            ModuleType.ScreenShare => "ScreenShare",
            _ => throw new ArgumentException($"Unsupported module: {module}")
        };
    }
}
