using Newtonsoft.Json;

public class PersistenceItem
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("data")]
    public byte[] Data { get; set; } = Array.Empty<byte>();
}