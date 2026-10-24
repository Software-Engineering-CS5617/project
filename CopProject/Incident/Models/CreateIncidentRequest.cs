namespace Incident_module;

public class CreateIncidentRequest
{
    public string Title { get; }
    public string Description { get; }

    public Guid CreatedBy { get; }
    public IncidentStatus Status { get; }
    public IncidentCategory Category { get; }
    public IncidentSeverity Severity { get; }
    public IncidentLocation Location { get; }

    private readonly List<IncidentEvidence> _evidences;
    public IReadOnlyList<IncidentEvidence> Evidences => _evidences.AsReadOnly();

    public CreateIncidentRequest(
        string title,
        string description,
        Guid createdBy,
        IncidentCategory category,
        IncidentSeverity severity,
        IncidentLocation location,
        List<IncidentEvidence> evidences
    )
    {
        Title = title;
        Description = description;
        CreatedBy = createdBy;
        Status = IncidentStatus.Reported;
        Category = category;
        Severity = severity;
        Location = location;
        _evidences = new List<IncidentEvidence>(evidences); // copy to ensure immutability
    }
}
