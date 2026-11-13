namespace Incidents;

/// <summary>
/// Evidence related to an Incident such as photos, videos or documents.
/// Record type as it is immutable.
/// </summary>
/// <param name="Id"></param>
/// <param name="Description"></param>
/// <param name="FilePath">Path from which user uploaded evidence.</param>
/// <param name="UploadTime"></param>
/// <param name="UploadedBy"></param>
/// <param name="StoragePath">Evidence file path stored in the file synchrnoizer directory.</param>
public record IncidentEvidence(
    Guid Id,
    string Description,
    string? FilePath,
    DateTime UploadTime,
    string UploadedBy,
    string? StoragePath = null
);
