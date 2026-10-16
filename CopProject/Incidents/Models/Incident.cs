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

    internal IReadOnlyList<Guid> AdjacentIncidentIds => Edges.AsReadOnly();

    internal IReadOnlyList<IChange> ChangeHistory => Changes.AsReadOnly();

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
        ArgumentNullException.ThrowIfNull(evidence);

        if (evidence.Count == 0) { return; }

        DateTime changedTime = DateTime.Now;

        foreach (IncidentEvidence item in evidence)
        {
            _evidence.Add(item);

            Changes.Add(new IncidentEvidenceAdd(
                $"Evidence added: {item.Description}",
                changedTime,
                item.UploadedBy));
        }

        UpdateTime = changedTime;
    }

    internal void UpdateStatus(IncidentStatus newStatus, Guid changedBy = default)
    {
        if (Status == newStatus) { return; }

        DateTime changedTime = DateTime.Now;
        IncidentStatus oldStatus = Status;

        Status = newStatus;

        Changes.Add(new StatusChange(
            $"Status changed from {oldStatus} to {newStatus}",
            changedTime,
            changedBy));

        UpdateTime = changedTime;
    }

    internal void UpdateCategory(IncidentCategory newCategory, Guid changedBy = default)
    {
        if (Category == newCategory) { return; }

        DateTime changedTime = DateTime.Now;
        IncidentCategory oldCategory = Category;

        Category = newCategory;

        Changes.Add(new CategoryChange(
            $"Category changed from {oldCategory} to {newCategory}",
            changedTime,
            changedBy));

        UpdateTime = changedTime;
    }

    internal void UpdateSeverity(IncidentSeverity newSeverity, Guid changedBy = default)
    {
        if (Severity == newSeverity) { return; }

        DateTime changedTime = DateTime.Now;
        IncidentSeverity oldSeverity = Severity;

        Severity = newSeverity;

        Changes.Add(new SeverityChange(
            $"Severity changed from {oldSeverity} to {newSeverity}",
            changedTime,
            changedBy));

        UpdateTime = changedTime;
    }

    internal void UpdateLocation(IncidentLocation location, Guid changedBy = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (Location.Latitude == location.Latitude &&
            Location.Longitude == location.Longitude &&
            Location.Address == location.Address)
        {
            return;
        }

        DateTime changedTime = DateTime.Now;
        IncidentLocation oldLocation = Location;

        string oldValue =
            $"({oldLocation.Latitude}, {oldLocation.Longitude}, {oldLocation.Address})";

        string newValue =
            $"({location.Latitude}, {location.Longitude}, {location.Address})";

        Location = location;

        Changes.Add(new LocationChange(
            $"Location changed from {oldValue} to {newValue}",
            changedTime,
            changedBy));

        UpdateTime = changedTime;
    }

    internal bool AddAdjacentIncident(Guid otherIncidentId)
    {
        if (otherIncidentId == Id || Edges.Contains(otherIncidentId))
        {
            return false;
        }

        Edges.Add(otherIncidentId);
        UpdateTime = DateTime.Now;

        return true;
    }


}
