
namespace Incidents;

public class CreateIncidentRequest
{
    public string Title { get; }
    public string Description { get; }

    public Guid CreatedBy { get; }
    public IncidentStatus Status { get; }
    public IncidentCategory Category { get; }
    public IncidentSeverity Severity { get; }
    public IncidentLocation Location { get; }
    public List<IncidentEvidence> Evidences { get; }

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
        this.Title = title;
        this.Description = description;
        this.CreatedBy = createdBy;
        this.Status = IncidentStatus.Reported;
        this.Category = category;
        this.Severity = severity;
        this.Location = location;
        this.Evidences = new List<IncidentEvidence>(evidences);
    }
}
