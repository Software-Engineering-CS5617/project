public static class CosmosSettings
{
    public const string DatabaseName = "PersistenceDB";

    public const string Endpoint =
        "https://cloud-cosmos.documents.azure.com:443/";

    public static string Key
    {
        get
        {
            string? key =
            Environment.GetEnvironmentVariable("COSMOS_KEY");

            if (key == null)
            {
                throw new InvalidOperationException(
                    "COSMOS_KEY is not set");
            }

            return key;
        }
    
    }

}