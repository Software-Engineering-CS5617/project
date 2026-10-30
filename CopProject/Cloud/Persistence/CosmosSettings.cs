/// <summary>
/// The database credentials go here
/// </summary>
internal static class CosmosSettings
{
    internal const string DatabaseName = "PersistenceDB";

    internal const string Endpoint =
        "https://cloud-cosmos.documents.azure.com:443/";

    internal static string Key
    {
        get {
            string? key =
            Environment.GetEnvironmentVariable("COSMOS_KEY") ?? throw new InvalidOperationException(
                    "COSMOS_KEY is not set");
            return key;
        }

    }

}
