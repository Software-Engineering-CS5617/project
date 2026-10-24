namespace Incident_module;

public partial class Incident
{
    internal void AddEvidence(List<IncidentEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        if (evidence.Count == 0) { return; }

        DateTime changedTime = DateTime.Now;

        foreach (IncidentEvidence item in evidence)
        {
            _evidence.Add(item);

            _changes.Add(new IncidentEvidenceAdd(
                $"Evidence id: {item.Id} , added: {item.Description}",
                changedTime,
                item.UploadedBy));
        }

        UpdateTime = changedTime;
    }

    internal void UpdateStatus(IncidentStatus newStatus, Guid changedBy)
    {
        if (Status == newStatus) { return; }

        DateTime changedTime = DateTime.Now;
        IncidentStatus oldStatus = Status;

        Status = newStatus;

        _changes.Add(new StatusChange(
            $"Status changed from {oldStatus} to {newStatus}",
            changedTime,
            changedBy));

        UpdateTime = changedTime;
    }

    internal void UpdateCategory(IncidentCategory newCategory, Guid changedBy)
    {
        if (Category == newCategory) { return; }

        DateTime changedTime = DateTime.Now;
        IncidentCategory oldCategory = Category;

        Category = newCategory;

        _changes.Add(new CategoryChange(
            $"Category changed from {oldCategory} to {newCategory}",
            changedTime,
            changedBy));

        UpdateTime = changedTime;
    }

    internal void UpdateSeverity(IncidentSeverity newSeverity, Guid changedBy)
    {
        if (Severity == newSeverity) { return; }

        DateTime changedTime = DateTime.Now;
        IncidentSeverity oldSeverity = Severity;

        Severity = newSeverity;

        _changes.Add(new SeverityChange(
            $"Severity changed from {oldSeverity} to {newSeverity}",
            changedTime,
            changedBy));

        UpdateTime = changedTime;
    }

    internal void UpdateLocation(IncidentLocation location, Guid changedBy)
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
            $"({oldLocation.Latitude},{oldLocation.Longitude}) " + oldLocation.Address ?? "";

        string newValue =
            $"({location.Latitude},{location.Longitude}) " + location.Address ?? "";

        Location = location;

        _changes.Add(new LocationChange(
            $"Location changed from {oldValue} to {newValue}",
            changedTime,
            changedBy));

        UpdateTime = changedTime;
    }

    internal static bool AddAdjacentIncident(Incident incident1, Incident incident2, Guid changedBy)
    {
        if (incident1.Id == incident2.Id
            || incident1._edges.Contains(incident2.Id)
            || incident2._edges.Contains(incident1.Id))
        {
            return false;
        }

        incident1._edges.Add(incident2.Id);
        incident2._edges.Add(incident1.Id);

        DateTime changedTime = DateTime.Now;

        IChange change = new IncidentCorrelationChange(
            $"Incident {incident1.Id} correlated with Incident {incident2.Id}",
            changedTime,
            changedBy
        );

        incident1._changes.Add(change);
        incident2._changes.Add(change);

        return true;
    }

    internal Incident Clone()
    {
        return new Incident(
            Id,
            Title,
            Description,
            CreatedBy,
            Location,
            Status,
            Category,
            Severity,
            CreationTime,
            UpdateTime,
            new List<IncidentEvidence>(_evidence),
            new List<IChange>(_changes),
            new List<Guid>(_edges)
        );
    }
}
