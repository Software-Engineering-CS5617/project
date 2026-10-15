namespace Incidents;

public class Incident<T>
{
    public Guid Id { get; }
    public string Title { get; }
    public string Description { get; }
    public Guid CreatedBy { get; }
    public IncidentLocation Location { get; }
    public IncidentStatus Status { get; }
    public IncidentCategory Category { get; }
    public IncidentSeverity Severity { get; }
    public DateTime CreationTime { get; }
    public DateTime UpdateTime { get; private set; }

    public List<IncidentEvidence> Evidence { get; private set; }
    public List<IChange<T>> Changes { get; private set; }

    private List<Guid> Edges { get; }

    public List<IncidentCorrelation> CorrelatedIncidents { get; }

    public Incident(
        string title,
        string description,
        Guid createdBy,
        IncidentCategory category,
        IncidentSeverity severity,
        IncidentLocation location
    )
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        CreatedBy = createdBy;
        Location = location;
        Status = IncidentStatus.Reported;
        Category = category;
        Severity = severity;
        CreationTime = DateTime.Now;
        UpdateTime = DateTime.Now;

        Evidence = [];
        Changes = [];
        Edges = [];
        CorrelatedIncidents = [];
    }

    internal void AddEvidence(List<IncidentEvidence> evidence)
    {
        Evidence.AddRange(evidence);
        UpdateTime = DateTime.Now;
    }
}
