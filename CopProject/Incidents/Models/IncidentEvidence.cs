namespace Incidents;

public sealed class IncidentEvidence
{
    public string Description { get; }
    public string? FilePath { get; }
    public DateTime UploadTime { get; }
    public Guid UploadedBy { get; }

    public IncidentEvidence(
        string description,
        string? filePath,
        Guid uploadedBy
    )
    {
        Description = description;
        FilePath = filePath;
        UploadedBy = uploadedBy;
        UploadTime = DateTime.Now;
    }
}
