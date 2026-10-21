namespace Incidents;

using Filesync;
using Persistence;

public class IncidentController : IIncidentController
{
    // TODO(@shreesh): IControllerService is to be used for getting Guid of the user using the Controller.
    private readonly IStorage _cloudStorageService;
    private readonly IFileOperations _fileSynchronizerService;
    private readonly Dictionary<Guid, Incident> _incidentMap;

    private IncidentController(
        IStorage cloudStorageService,
        IFileOperations fileSynchronizerService,
        Dictionary<Guid, Incident> incidentMap
    )
    {
        _cloudStorageService = cloudStorageService;
        _fileSynchronizerService = fileSynchronizerService;
        _incidentMap = incidentMap;
    }

    public async static Task<IncidentController> CreateAsync(IStorage cloudStorageService, IFileOperations fileSynchronizerService)
    {
        try
        {
            (bool success, List<string> keys) = await cloudStorageService.GetAllKeys();

            if (!success)
            {
                throw new InvalidOperationException("Failed to retrieve keys from cloud storage.");
            }

            (success, Dictionary<string, byte[]> data) = await cloudStorageService.ReadAll(keys.ToArray());

            if (!success)
            {
                throw new InvalidOperationException("Failed to read data from cloud storage.");
            }

            Dictionary<Guid, Incident> incidentMap = [];

            foreach ((string id, byte[] incidentBytes) in data)
            {
                // TODO(@shreesh): add XML deserialization errors later
                Incident incident = Incidents.IncidentSerializer.Deserialize(incidentBytes);
                incidentMap[incident.Id] = incident;
            }

            return new IncidentController(
                cloudStorageService,
                fileSynchronizerService,
                incidentMap
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize IncidentController:", ex);
        }
    }

    public async Task<Incident> CreateIncident(CreateIncidentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Incident incident = new Incident(
            request.Title,
            request.Description,
            request.CreatedBy,
            request.Category,
            request.Severity,
            request.Location,
            request.Evidences.ToList()
        );

        bool success;
        try
        {
            success = await _cloudStorageService.Create(
                incident.Id.ToString(),
                Incidents.IncidentSerializer.Serialize(incident)
            );

            if (!success)
            {
                throw new InvalidOperationException("Failed to persist incident to cloud storage.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to persist incident to cloud storage.", ex);
        }

        _incidentMap.Add(incident.Id, incident);

        return incident;
    }

    public async Task<Incident> GetIncidentById(Guid id)
    {
        Incident incident = FindIncident(id) ?? throw new KeyNotFoundException($"Incident with ID {id} not found.");

        return incident;
    }

    public async Task<List<Incident>> GetAllIncidents(IncidentFilter? filter = null)
    {
        return _incidentMap.Values.
            Where(i => filter is null || IncidentFilterHelper.Matches(i, filter)).
            ToList();
    }

    private Incident? FindIncident(Guid id)
    {
        return _incidentMap.GetValueOrDefault(id);
    }

    public async Task<List<Incident>> GetCorrelatedIncidents(Guid id)
    {
        Incident _ = FindIncident(id) ?? throw new KeyNotFoundException($"Incident with ID {id} not found.");

        List<Incident> correlated = [];
        Queue<Guid> queue = [];
        HashSet<Guid> visited = [];

        visited.Add(id);
        queue.Enqueue(id);

        while (queue.Count > 0)
        {
            Guid currentId = queue.Dequeue();
            Incident current = FindIncident(currentId) ?? throw new KeyNotFoundException($"Incident with ID {id} not found.");

            foreach (Guid adjacentId in current.AdjacentIncidentIds)
            {
                if (visited.Add(adjacentId))
                {
                    queue.Enqueue(adjacentId);

                    Incident adjacent = FindIncident(adjacentId) ?? throw new KeyNotFoundException($"Incident with ID {id} not found.");

                    correlated.Add(adjacent);
                }
            }
        }

        return correlated;
    }

    public async Task<bool> AddEvidence(Incident incident, List<IncidentEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(evidence);

        incident.AddEvidence(evidence);

        // TODO: Persist the updated incident using CloudService.

        return true;
    }

    public async Task<bool> UpdateStatus(Incident incident, IncidentStatus newStatus)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!Enum.IsDefined(typeof(IncidentStatus), newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        }

        incident.UpdateStatus(newStatus, Guid.Empty);

        // TODO: Persist the incident to cloud storage.

        return true;
    }

    public async Task<bool> UpdateCategory(Incident incident, IncidentCategory newCategory)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!Enum.IsDefined(typeof(IncidentCategory), newCategory))
        {
            throw new ArgumentOutOfRangeException(nameof(newCategory));
        }

        incident.UpdateCategory(newCategory, Guid.Empty);

        // TODO: Persist the incident to cloud storage.

        return true;
    }

    public async Task<bool> UpdateSeverity(Incident incident, IncidentSeverity newSeverity)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!Enum.IsDefined(typeof(IncidentSeverity), newSeverity))
        {
            throw new ArgumentOutOfRangeException(nameof(newSeverity));
        }

        incident.UpdateSeverity(newSeverity, Guid.Empty);

        // TODO: Persist the incident to cloud storage.

        return true;
    }

    public async Task<bool> UpdateLocation(Incident incident, IncidentLocation location)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(location);

        incident.UpdateLocation(location, Guid.Empty);

        // TODO: Persist the incident to cloud storage.

        return true;
    }

    public async Task<bool> CorrelateIncident(Incident incident1, Incident incident2)
    {
        ArgumentNullException.ThrowIfNull(incident1);
        ArgumentNullException.ThrowIfNull(incident2);

        // TODO(@shreesh): Check if the incidents exist in the first place.

        if (incident1.Id == incident2.Id)
        {
            return false;
        }

        List<Incident> correlatedIncidents1 = await GetCorrelatedIncidents(incident1.Id);
        List<Incident> correlatedIncidents2 = await GetCorrelatedIncidents(incident2.Id);

        if (correlatedIncidents1.Any(i => i.Id == incident2.Id)
            || correlatedIncidents2.Any(i => i.Id == incident1.Id))
        {
            return false;
        }

        bool added1 = incident1.AddAdjacentIncident(incident2.Id, Guid.Empty);
        bool added2 = incident2.AddAdjacentIncident(incident1.Id, Guid.Empty);

        // TODO(@shreesh): Persist the updated incidents to cloud storage
        // and create copy of Incident before update.

        return added1 && added2;
    }
}
