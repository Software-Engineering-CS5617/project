namespace Incident_module;

// <summary>
// Evidence related to an Incident such as photos, videos or documents.
// Record type as it is immutable.
// </summary>
public record IncidentEvidence(
    Guid Id,
    string Description,
    string? FilePath,
    DateTime UploadTime,
    Guid UploadedBy
);
