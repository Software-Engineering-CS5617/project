namespace Incidents;

public class Incident
{
    public Guid Id { get; }
    public string Title { get; }
    public string Description { get; }
    public Guid CreatedBy { get; }
    public IncidentLocation Location { get; private set; }
    public IncidentStatus Status { get; private set; }
    public IncidentCategory Category { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public DateTime CreationTime { get; }
    public DateTime UpdateTime { get; private set; }

    private readonly List<IncidentEvidence> _evidence;

    public IReadOnlyList<IncidentEvidence> Evidence => _evidence.AsReadOnly();

    private List<IChange> Changes { get; set; }

    private List<Guid> Edges { get; }

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
        _evidence = [];
        Changes = [];
        Edges = [];
    }

    internal Incident(
        Guid id,
        string title,
        string description,
        Guid createdBy,
        IncidentLocation location,
        IncidentStatus status,
        IncidentCategory category,
        IncidentSeverity severity,
        DateTime creationTime,
        DateTime updateTime,
        List<IncidentEvidence> evidence,
        List<IChange> changes,
        List<Guid> edges
    )
    {
        Id = id;
        Title = title;
        Description = description;
        CreatedBy = createdBy;
        Location = location;
        Status = status;
        Category = category;
        Severity = severity;
        CreationTime = creationTime;
        UpdateTime = updateTime;
        _evidence = new List<IncidentEvidence>(evidence);
        Changes = new List<IChange>(changes);
        Edges = new List<Guid>(edges);
    }

    internal void AddEvidence(List<IncidentEvidence> evidence)
    {
        _evidence.AddRange(evidence);
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
