using Newtonsoft.Json;

namespace Cloud.Persistence;
/// <summary>
/// C# model representing a document stored in a Cosmos DB container
/// Each item is a key-value pair
/// Along with system-managed metadata
/// </summary>
internal class PersistenceItem
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

    /// <summary>
    /// _etag is the version stamp for a cosmos db item. 
    /// Used to handle concurrent updates
    /// Gets or sets the system generated ETag (_etag) of the item
    /// </summary>
    [JsonProperty("_etag")]
    public string Etag { get; set; } = string.Empty;

    /// <summary>
    /// Represents the time when the item was last modified.
    /// Gets or sets the system generated Unix timestamp (_ts) of the cosmos db item
    /// </summary>
    [JsonProperty("_ts")]
    public string TimeStamp { get; set; } = string.Empty;
}
