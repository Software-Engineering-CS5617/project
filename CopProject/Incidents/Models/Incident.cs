namespace Incidents;

public partial class Incident
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

    private readonly List<IChange> _changes;

    private readonly List<Guid> _edges;

    public IReadOnlyList<Guid> AdjacentIncidentIds => _edges.AsReadOnly();

    public IReadOnlyList<IChange> ChangeHistory => _changes.AsReadOnly();

    internal Incident(
        string title,
        string description,
        Guid createdBy,
        IncidentCategory category,
        IncidentSeverity severity,
        IncidentLocation location,
        List<IncidentEvidence>? evidence = null
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
        _evidence = evidence != null ? new List<IncidentEvidence>(evidence) : [];
        _changes = [];
        _edges = [];
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
        _changes = new List<IChange>(changes);
        _edges = new List<Guid>(edges);
    }
}
