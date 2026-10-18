using Newtonsoft.Json;

/// <summary>
/// Represents one item stored in a Cosmos DB container
/// Each item is a key-value pair
/// </summary>
public class PersistenceItem
{
    /// <summary>
    /// Gets or sets the unique key of the item. 
    /// It is also used as the partition key in Cosmos DB
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data as a byte array. 
    /// </summary>
    [JsonProperty("data")]
    public byte[] Data { get; set; } = [];
}
