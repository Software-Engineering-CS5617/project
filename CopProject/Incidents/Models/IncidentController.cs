namespace Incidents;

public class IncidentController : IIncidentController
{
    // IFileSyncService and ICloudService are to be used for file synchronization and cloud storage.

    private readonly List<Incident> _incidents = [];
    public Task<Incident> CreateIncident(CreateIncidentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Incident incident = new Incident(
            request.Title,
            request.Description,
            request.CreatedBy,
            request.Category,
            request.Severity,
            request.Location);

        if (request.Evidences.Count > 0)
        {
            incident.AddEvidence(request.Evidences.ToList());
        }
        // TODO : File sync for evidences.

        _incidents.Add(incident);

        // TODO: Persist the serialized incident to cloud storage.

        return Task.FromResult(incident);

    }

    public Task<Incident> GetIncidentById(Guid id)
    {
        Incident incident = _incidents.FirstOrDefault(i => i.Id == id)
                            ?? throw new KeyNotFoundException($"Incident with ID {id} not found.");

        return Task.FromResult(incident);

    }

    public Task<List<Incident>> GetAllIncidents(IncidentFilter? filter = null)
    {
        List<Incident> result = [];

        foreach (Incident incident in _incidents)
        {
            if (filter is null || IncidentFilterHelper.Matches(incident, filter))
            {
                result.Add(incident);
            }
        }

        return Task.FromResult(result);
    }

    private Incident? FindIncident(Guid id)
    {
        foreach (Incident incident in _incidents)
        {
            if (incident.Id == id)
            {
                return incident;
            }
        }

        return null;
    }
    public Task<List<Incident>> GetCorrelatedIncidents(Guid id)
    {
        List<Incident> correlated = [];
        Queue<Guid> queue = [];
        HashSet<Guid> visited = [];

        visited.Add(id);
        queue.Enqueue(id);

        while (queue.Count > 0)
        {
            Guid currentId = queue.Dequeue();
            Incident? current = FindIncident(currentId);

            if (current is null)
            {
                continue;
            }

            foreach (Guid adjacentId in current.AdjacentIncidentIds)
            {
                if (visited.Add(adjacentId))
                {
                    queue.Enqueue(adjacentId);

                    Incident? adjacent = FindIncident(adjacentId);

                    if (adjacent is not null)
                    {
                        correlated.Add(adjacent);
                    }
                }
            }
        }

        return Task.FromResult(correlated);
    }

    public Task<bool> AddEvidence(Incident incident, List<IncidentEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(evidence);

        incident.AddEvidence(evidence);

        // TODO: Synchronize evidence files using FileSyncService.
        // TODO: Persist the updated incident using CloudService.

        return Task.FromResult(true);
    }

    public Task<bool> UpdateStatus(Incident incident, IncidentStatus newStatus)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!Enum.IsDefined(typeof(IncidentStatus), newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        }

        incident.UpdateStatus(newStatus);

        // TODO: Persist the incident to cloud storage.

        return Task.FromResult(true);
    }

    public Task<bool> UpdateCategory(Incident incident, IncidentCategory newCategory)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!Enum.IsDefined(typeof(IncidentCategory), newCategory))
        {
            throw new ArgumentOutOfRangeException(nameof(newCategory));
        }

        incident.UpdateCategory(newCategory);

        // TODO: Persist the incident to cloud storage.

        return Task.FromResult(true);
    }

    public Task<bool> UpdateSeverity(Incident incident, IncidentSeverity newSeverity)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!Enum.IsDefined(typeof(IncidentSeverity), newSeverity))
        {
            throw new ArgumentOutOfRangeException(nameof(newSeverity));
        }

        incident.UpdateSeverity(newSeverity);

        // TODO: Persist the incident to cloud storage.

        return Task.FromResult(true);
    }

    public Task<bool> UpdateLocation(Incident incident, IncidentLocation location)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(location);

        incident.UpdateLocation(location);

        // TODO: Persist the incident to cloud storage.

        return Task.FromResult(true);
    }

    public Task<bool> CorrelateIncident(Incident incident1, Incident incident2)
    {
        ArgumentNullException.ThrowIfNull(incident1);
        ArgumentNullException.ThrowIfNull(incident2);

        if (incident1.Id == incident2.Id)
        {
            return Task.FromResult(false);
        }

        bool added1 = incident1.AddAdjacentIncident(incident2.Id);
        bool added2 = incident2.AddAdjacentIncident(incident1.Id);

        return Task.FromResult(added1 || added2);
    }
}
