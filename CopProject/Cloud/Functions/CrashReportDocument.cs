using System.Text.Json.Serialization;

namespace Cloud.Functions;

/// <summary>
/// The ID of a new document of the CrashReports container. The report XML itself is read through IStorage.
/// </summary>
public sealed class CrashReportDocument
{
    /// <summary>
    /// Gets or sets the document ID, which is the report ID. The attribute is needed only because the trigger binding is JSON.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
