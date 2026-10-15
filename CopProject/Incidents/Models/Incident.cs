namespace Incidents;

public class Incident<T>
{
    public Guid Id { get; }
    public string Title { get; }
    public string Description { get; }
    public Guid CreatedBy { get; }
    public IncidentLocation Location { get; private set; }
    public IncidentStatus Status { get; private set; }
    public IncidentCategory Category { get; private set; }
    public IncidentSeverity Severity { get; private set;  }
    public DateTime CreationTime { get; }
    public DateTime UpdateTime { get; private set; }

    public List<IncidentEvidence> Evidence { get; private set; }
    public List<IChange<T>> Changes { get; private set; }

    private List<Guid> Edges { get; }

    public List<IncidentCorrelation> CorrelatedIncidents { get; }

    internal Incident(
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

    internal void UpdateStatus(IncidentStatus newStatus)
    {
        Status = newStatus;
        UpdateTime = DateTime.Now;
    }

    internal void UpdateCategory(IncidentCategory newCategory)
    {
        Category = newCategory;
        UpdateTime = DateTime.Now;
    }

    internal void UpdateSeverity(IncidentSeverity newSeverity)
    {
        Severity = newSeverity;
        UpdateTime = DateTime.Now;
    }

    internal void UpdateLocation(IncidentLocation location)
    {
        Location = location;
        UpdateTime = DateTime.Now;
    }


}
