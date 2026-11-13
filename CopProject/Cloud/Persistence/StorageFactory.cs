using Microsoft.Azure.Cosmos;

namespace Cloud.Persistence;

/// <summary>
/// Creates IStorage instances for application modules
/// All modules share a Cosmosclient and each module gets its own container
/// </summary>
public static class StorageFactory
{
    // A single instance of CosmosClient is created and reused for all storage instances.

    // Delay construction so container-name mapping can be tested without Cosmos credentials.
    private static readonly Lazy<CosmosClient> s_cosmosClient = new(
        () => new CosmosClient(CosmosSettings.Endpoint, CosmosSettings.Key));

    /// <summary>
    /// This method asynchronously creates an IStorage instance for the specified module type if it doesn't exist .
    /// </summary>
    /// <param name="module"></param>
    /// <returns>IStorage instance ready to use</returns>
    public static Task<IStorage> CreateAsync(ModuleType module)
    {
        return CreateAsync(module, s_cosmosClient);
    }

    // The overload lets tests provide a mocked CosmosClient without creating
    // a real client or connecting to a Cosmos account.
    internal static async Task<IStorage> CreateAsync(
        ModuleType module,
        Lazy<CosmosClient> cosmosClient)
    {
        string containerName = GetContainerName(module);

        CosmosStorage storage = new CosmosStorage(
            cosmosClient.Value,
            CosmosSettings.DatabaseName,
            containerName);

        await storage.InitializeAsync();

        return storage;
    }

    internal static string GetContainerName(ModuleType module)
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
            ModuleType.CrashReports => "CrashReports",
            ModuleType.CrashGroups => "CrashGroups",
            _ => throw new ArgumentException($"Unsupported module: {module}")
        };
    }
}
