
namespace Incidents;

public class CreateIncidentRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public IncidentStatus Status { get; set; }
    public IncidentCategory Category { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentLocation Location { get; set; }
    public List<IncidentEvidence> Evidences { get; set; }

    public CreateIncidentRequest(
        string title,
        string description,
        IncidentCategory category,
        IncidentSeverity severity,
        IncidentLocation location,
        List<IncidentEvidence> evidences
    )
    {
        this.Title = title;
        this.Description = description;
        this.Status = IncidentStatus.Reported;
        this.Category = category;
        this.Severity = severity;
        this.Location = location;
        this.Evidences = new List<IncidentEvidence>(evidences);
    }
}
