namespace Incident_module;

using System.Security.Policy;
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
                try
                {
                    Incident incident = Incident_module.IncidentSerializer.Deserialize(incidentBytes);
                    incidentMap[incident.Id] = incident;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to deserialize incident with ID {id}: ", ex);
                }
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
                Incident_module.IncidentSerializer.Serialize(incident)
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

    private bool IncidentResolved(Incident incident)
    {
        return incident.Status == IncidentStatus.Resolved;
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

        if (IncidentResolved(incident))
        {
            throw new InvalidOperationException("Cannot add evidence to a resolved incident.");
        }

        Incident clone = incident.Clone();

        clone.AddEvidence(evidence);

        try
        {
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                throw new InvalidOperationException("Failed to read ETag from cloud storage.");
            }

            success = await _cloudStorageService.Update(
                clone.Id.ToString(),
                Incident_module.IncidentSerializer.Serialize(clone),
                etag
           );

            if (!success)
            {
                throw new InvalidOperationException("Failed to update Incident to cloud storage.");
            }

            incident.AddEvidence(evidence);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to read ETag from cloud storage:", ex);
        }
    }

    public async Task<bool> UpdateStatus(Incident incident, IncidentStatus newStatus)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!Enum.IsDefined(typeof(IncidentStatus), newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        }

        Incident clone = incident.Clone();

        clone.UpdateStatus(newStatus, Guid.Empty);

        try
        {
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                throw new InvalidOperationException("Failed to read ETag from cloud storage.");
            }

            success = await _cloudStorageService.Update(
                clone.Id.ToString(),
                Incident_module.IncidentSerializer.Serialize(clone),
                etag
           );

            if (!success)
            {
                throw new InvalidOperationException("Failed to update Incident to cloud storage.");
            }

            incident.UpdateStatus(newStatus, Guid.Empty);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to read ETag from cloud storage:", ex);
        }
    }

    public async Task<bool> UpdateCategory(Incident incident, IncidentCategory newCategory)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (IncidentResolved(incident))
        {
            throw new InvalidOperationException("Cannot update category of a resolved incident.");
        }

        if (!Enum.IsDefined(typeof(IncidentCategory), newCategory))
        {
            throw new ArgumentOutOfRangeException(nameof(newCategory));
        }

        Incident clone = incident.Clone();

        clone.UpdateCategory(newCategory, Guid.Empty);

        try
        {
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                throw new InvalidOperationException("Failed to read ETag from cloud storage.");
            }

            success = await _cloudStorageService.Update(
                clone.Id.ToString(),
                   Incident_module.IncidentSerializer.Serialize(clone),
                etag
           );

            if (!success)
            {
                throw new InvalidOperationException("Failed to update Incident to cloud storage.");
            }

            incident.UpdateCategory(newCategory, Guid.Empty);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to read ETag from cloud storage:", ex);
        }
    }

    public async Task<bool> UpdateSeverity(Incident incident, IncidentSeverity newSeverity)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (IncidentResolved(incident))
        {
            throw new InvalidOperationException("Cannot update severity of a resolved incident.");
        }

        if (!Enum.IsDefined(typeof(IncidentSeverity), newSeverity))
        {
            throw new ArgumentOutOfRangeException(nameof(newSeverity));
        }

        Incident clone = incident.Clone();

        clone.UpdateSeverity(newSeverity, Guid.Empty);

        try
        {
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                throw new InvalidOperationException("Failed to read ETag from cloud storage.");
            }

            success = await _cloudStorageService.Update(
                clone.Id.ToString(),
                Incident_module.IncidentSerializer.Serialize(clone),
                etag
           );

            if (!success)
            {
                throw new InvalidOperationException("Failed to update Incident to cloud storage.");
            }

            incident.UpdateSeverity(newSeverity, Guid.Empty);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to read ETag from cloud storage:", ex);
        }
    }

    public async Task<bool> UpdateLocation(Incident incident, IncidentLocation location)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(location);

        if (IncidentResolved(incident))
        {
            throw new InvalidOperationException("Cannot update location of a resolved incident.");
        }

        Incident clone = incident.Clone();

        clone.UpdateLocation(location, Guid.Empty);

        try
        {
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                throw new InvalidOperationException("Failed to read ETag from cloud storage.");
            }

            success = await _cloudStorageService.Update(
                clone.Id.ToString(),
                Incident_module.IncidentSerializer.Serialize(clone),
                etag
           );

            if (!success)
            {
                throw new InvalidOperationException("Failed to update Incident to cloud storage.");
            }

            incident.UpdateLocation(location, Guid.Empty);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to update Incident to cloud storage:", ex);
        }
    }

    public async Task<bool> CorrelateIncident(Incident incident1, Incident incident2)
    {
        ArgumentNullException.ThrowIfNull(incident1);
        ArgumentNullException.ThrowIfNull(incident2);

        if (IncidentResolved(incident1) && IncidentResolved(incident2))
        {
            throw new InvalidOperationException("Cannot correlate resolved incidents.");
        }

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

        Incident incident1Clone = incident1.Clone();
        Incident incident2Clone = incident2.Clone();

        bool success = Incident.AddAdjacentIncident(incident1Clone, incident2Clone, Guid.Empty);

        // TODO(@shreesh): Change the signature to void and throw error
        if (!success)
        {
            return false;
        }

        try
        {
            // TODO(@shreesh): Update to ReadAllEtags later
            (bool success1, string etag1) = await _cloudStorageService.ReadEtag(incident1Clone.Id.ToString());
            (bool success2, string etag2) = await _cloudStorageService.ReadEtag(incident2Clone.Id.ToString());

            if (!success1 || !success2)
            {
                throw new InvalidOperationException("Failed to read ETag from cloud storage.");
            }

            Dictionary<string, byte[]> itemsToUpdate = new Dictionary<string, byte[]>
            {
                { incident1Clone.Id.ToString(), Incident_module.IncidentSerializer.Serialize(incident1Clone) },
                { incident2Clone.Id.ToString(), Incident_module.IncidentSerializer.Serialize(incident2Clone) }
            };

            success = await _cloudStorageService.UpdateAll(itemsToUpdate);

            if (!success)
            {
                throw new InvalidOperationException("Failed to update Incident to cloud storage.");
            }

            Incident.AddAdjacentIncident(incident1, incident2, Guid.Empty);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to update Incident to cloud storage:", ex);
        }
    }
}
