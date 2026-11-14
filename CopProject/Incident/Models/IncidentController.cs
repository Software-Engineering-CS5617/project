namespace Incidents;

using System.Diagnostics;
using Cloud.Persistence;
using FileSynchroniser.Models;

public class IncidentController : IIncidentController
{
    private readonly string _currentUserId;
    private readonly string _relativePath = "Incidents";
    private readonly IStorage _cloudStorageService;
    private readonly IFileOperations _fileSynchronizerService;
    private readonly Dictionary<Guid, Incident> _incidentMap;

    private IncidentController(
        string currentUserId,
        IStorage cloudStorageService,
        IFileOperations fileSynchronizerService,
        Dictionary<Guid, Incident> incidentMap
    )
    {
        _currentUserId = currentUserId;
        _cloudStorageService = cloudStorageService;
        _fileSynchronizerService = fileSynchronizerService;
        _incidentMap = incidentMap;
    }

    public async static Task<IncidentController> CreateAsync(string currentUserId, IStorage cloudStorageService, IFileOperations fileSynchronizerService)
    {
        try
        {
            Trace.TraceInformation($"[Incidents]: Initializing IncidentController.");
            (bool success, List<string> keys) = await cloudStorageService.GetAllKeys();

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to retrieve keys from cloud storage.");
                throw new InvalidOperationException("Failed to retrieve keys from cloud storage.");
            }

            (success, Dictionary<string, byte[]> data) = await cloudStorageService.ReadAll(keys.ToArray());

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to read data from cloud storage.");
                throw new InvalidOperationException("Failed to read data from cloud storage.");
            }

            Dictionary<Guid, Incident> incidentMap = [];

            foreach ((string id, byte[] incidentBytes) in data)
            {
                try
                {
                    Incident incident = Incidents.IncidentSerializer.Deserialize(incidentBytes);
                    incidentMap[incident.Id] = incident;
                }
                catch (Exception)
                {
                    Trace.TraceWarning($"[Incidents]: Failed to deserialize incident with ID {id}. Skipping this incident.");
                }
            }

            Trace.TraceInformation($"[Incidents]: IncidentController initialized successfully with {incidentMap.Count} incidents.");
            return new IncidentController(
                currentUserId,
                cloudStorageService,
                fileSynchronizerService,
                incidentMap
            );
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to initialize IncidentController: {ex.Message}");
            throw new InvalidOperationException("Failed to initialize IncidentController:", ex);
        }
    }


    /// <summary>
    /// Redacts/Delete files that were saved by FileSynchronizer.
    /// </summary>
    /// <param name="request">Evidences to delete</param>
    /// <returns>true if all files were delete successfully else false.</returns>
    private bool RedactFilesSave(List<IncidentEvidence> request)
    {
        try
        {
            IReadOnlyDictionary<string, bool> results = _fileSynchronizerService.DeleteFile(
                request.Select(e => $"{_relativePath}/{e.Id}"));

            return results.Values.All(deleted => deleted);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a new Incident.
    /// </summary>
    /// <param name="request"> Information to create the Incident.</param>
    /// <returns>The created Incident.</returns>
    /// <exception cref="InvalidOperationException">Throws InvalidOperationException if Incident can't be created.</exception>
    public async Task<Incident> CreateIncident(CreateIncidentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            Dictionary<string, byte[]> files = request.Evidences.ToDictionary(
                e => $"{_relativePath}/{e.Item1.Id}{Path.GetExtension(e.Item1.FilePath)}",
                e => e.Item2);

            Trace.TraceInformation($"[Incidents]: Saving {files.Count} evidence files to file synchronizer service for incident with title {request.Title} created by {request.CreatedBy}");
            IReadOnlyDictionary<string, bool> saveResults = _fileSynchronizerService.SaveFile(files);

            if (saveResults.Count != files.Count || saveResults.Values.Any(saved => !saved))
            {
                Trace.TraceError($"[Incidents]: Failed to save evidence files for incident with title {request.Title} created by {request.CreatedBy} to file synchronizer service.");
                throw new InvalidOperationException("Failed to save evidence files to file synchronizer service.");
            }

            // set storage path in which files will be stored by file synchronizer service.
            List<IncidentEvidence> evidenceMeta = request.Evidences.Select(e =>
                e.Item1.FilePath is not null
                ? e.Item1 with { StoragePath = $"{_relativePath}/{e.Item1.Id}{Path.GetExtension(e.Item1.FilePath)}" }
                : e.Item1).ToList();

            Incident incident = new Incident(
                request.Title,
                request.Description,
                request.CreatedBy,
                request.Category,
                request.Severity,
                request.Location,
                evidenceMeta
            );

            Trace.TraceInformation($"[Incidents]: Persisting new created incident with ID '{incident.Id}' to cloud storage.");
            bool success = await _cloudStorageService.Create(
                incident.Id.ToString(),
                Incidents.IncidentSerializer.Serialize(incident)
            );

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to persist new incident with ID {incident.Id} to cloud storage. Attempting to redact saved evidence files.");
                bool result = RedactFilesSave(evidenceMeta);

                if (result)
                {
                    Trace.TraceInformation($"[Incidents]: Successfully redacted saved evidence files after failed cloud persistence for incident with ID {incident.Id}.");
                    throw new InvalidOperationException($"Failed to persist incident with ID {incident.Id} to cloud storage.");
                }

                Trace.TraceError($"[Incidents]: Failed to redact saved evidence files for incident with ID {incident.Id}. File synchronizer directory is in inconsistent state.");
                throw new InvalidOperationException($"Failed to persist incident with ID {incident.Id} to cloud storage." +
                    " File syncronizer directory is in inconsistent state");
            }

            _incidentMap.Add(incident.Id, incident);

            return incident;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to create incident with title {request.Title} by {request.CreatedBy}.");
            throw new InvalidOperationException($"Failed to create incident with title {request.Title} by {request.CreatedBy}", ex);
        }
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

    /// <summary>
    /// Refetches Incidents from Cloud Storage and updates the local cache.
    /// </summary>
    public async Task Refresh()
    {
        try
        {
            Trace.TraceInformation($"[Incidents]: Fetching all incident keys from cloud storage for refresh.");
            (bool success, List<string> keys) = await _cloudStorageService.GetAllKeys();

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to retrieve keys from cloud storage during refresh.");
                throw new InvalidOperationException("Failed to refresh from cloud storage.");
            }

            (success, Dictionary<string, byte[]> data) = await _cloudStorageService.ReadAll(keys.ToArray());

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to read incident data from cloud storage during refresh.");
                throw new InvalidOperationException("Failed to refresh from cloud storage.");
            }

            _incidentMap.Clear();

            foreach ((string id, byte[] incidentBytes) in data)
            {
                try
                {
                    Incident incident = Incidents.IncidentSerializer.Deserialize(incidentBytes);
                    _incidentMap[incident.Id] = incident;
                }
                catch (Exception)
                {
                    Trace.TraceWarning($"[Incidents]: Failed to deserialize incident with ID {id} during refresh. Skipping this incident.");
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to refresh incidents from cloud storage: {ex.Message}");
            throw new InvalidOperationException("Failed to refresh from cloud storage: ", ex);
        }
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

    public async Task<bool> AddEvidence(Incident incident, List<(IncidentEvidence, byte[])> evidences)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(evidences);

        if (IncidentResolved(incident))
        {
            throw new InvalidOperationException("Cannot add evidence to a resolved incident.");
        }

        Incident clone = incident.Clone();

        try
        {
            Dictionary<string, byte[]> files = evidences.ToDictionary(
                    e => $"{_relativePath}/{e.Item1.Id}{Path.GetExtension(e.Item1.FilePath)}",
                    e => e.Item2);

            Trace.TraceInformation($"[Incidents]: Saving {files.Count} new evidence files to file synchronizer service.");
            IReadOnlyDictionary<string, bool> saveResults = _fileSynchronizerService.SaveFile(files);

            if (saveResults.Count != files.Count || saveResults.Values.Any(saved => !saved))
            {
                Trace.TraceError($"[Incidents]: Failed to save new evidence files for incident with ID {clone.Id} to file synchronizer service.");
                throw new InvalidOperationException($"Failed to save new evidence files for incident with ID {clone.Id} to file synchronizer service.");
            }

            // set storage path in which files will be stored by file synchronizer service.
            List<IncidentEvidence> evidenceMeta = evidences.Select(e =>
                e.Item1.FilePath is not null
                ? e.Item1 with { StoragePath = $"{_relativePath}/{e.Item1.Id}{Path.GetExtension(e.Item1.FilePath)}" }
                : e.Item1).ToList();

            clone.AddEvidence(evidenceMeta);

            Trace.TraceInformation($"[Incidents]: Reading ETag for incident with ID {clone.Id} in cloud storage.");
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to read ETag for incident with ID {clone.Id} in cloud storage.");
                throw new InvalidOperationException($"Failed to read ETag for incident with ID {clone.Id} in cloud storage.");
            }

            Trace.TraceInformation($"[Incidents]: Updating incident with ID {clone.Id} with ETag {etag} with evidence in cloud storage.");
            if (!await _cloudStorageService.Update(
                clone.Id.ToString(),
                Incidents.IncidentSerializer.Serialize(clone),
                etag
            ))
            {
                Trace.TraceError($"[Incidents]: Failed to update incident with ID {clone.Id} with ETag {etag} with evidence in cloud storage.");
                throw new InvalidOperationException($"Failed to update incident with ID {clone.Id} with ETag {etag} with evidence in cloud storage.");
            }

            incident.AddEvidence(evidenceMeta);

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to add evidence to incident with ID {incident.Id} in cloud storage.");
            throw new InvalidOperationException($"Failed to add evidence to incident with ID {incident.Id} in cloud storage: ", ex);
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

        clone.UpdateStatus(newStatus, _currentUserId);

        try
        {
            Trace.TraceInformation($"[Incidents]: Reading ETag for incident with ID {clone.Id} in cloud storage.");
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to read ETag for incident with ID {clone.Id} in cloud storage.");
                throw new InvalidOperationException($"Failed to read ETag for incident with ID {clone.Id} in cloud storage.");
            }

            Trace.TraceInformation($"[Incidents]: Updating incident with ID {clone.Id} with ETag {etag} in cloud storage.");
            if (!await _cloudStorageService.Update(
                clone.Id.ToString(),
                Incidents.IncidentSerializer.Serialize(clone),
                etag
            ))
            {
                Trace.TraceError($"[Incidents]: Failed to update status for incident with ID {clone.Id} with ETag {etag} in cloud storage.");
                throw new InvalidOperationException($"Failed to update status for incident with ID {clone.Id} with ETag {etag} in cloud storage");
            }

            incident.UpdateStatus(newStatus, _currentUserId);

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to update status for incident with ID {clone.Id} in cloud storage.");
            throw new InvalidOperationException($"Failed to update status for incident with ID {clone.Id} in cloud storage: ", ex);
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

        clone.UpdateCategory(newCategory, _currentUserId);

        try
        {
            Trace.TraceInformation($"[Incidents]: Reading ETag for incident with ID {clone.Id} in cloud storage.");
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to read ETag for incident with ID {clone.Id} in cloud storage.");
                throw new InvalidOperationException($"Failed to read ETag for incident with ID {clone.Id} in cloud storage.");
            }

            Trace.TraceInformation($"[Incidents]: Updating incident with ID {clone.Id} with ETag {etag} in cloud storage.");
            if (!await _cloudStorageService.Update(
                clone.Id.ToString(),
                   Incidents.IncidentSerializer.Serialize(clone),
                etag
            ))
            {
                Trace.TraceError($"[Incidents]: Failed to update category for incident with ID {clone.Id} with ETag {etag} in cloud storage.");
                throw new InvalidOperationException($"Failed to update category for incident with ID {clone.Id} with ETag {etag} in cloud storage.");
            }

            incident.UpdateCategory(newCategory, _currentUserId);

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to update category to {newCategory} for incident with ID {clone.Id} in cloud storage.");
            throw new InvalidOperationException($"Failed to update category to {newCategory} for incident with ID {clone.Id} in cloud storage: ", ex);
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

        clone.UpdateSeverity(newSeverity, _currentUserId);

        try
        {
            Trace.TraceInformation($"[Incidents]: Reading ETag for incident with ID {clone.Id} in cloud storage.");
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to read ETag for incident with ID {clone.Id} in cloud storage.");
                throw new InvalidOperationException("Failed to read ETag from cloud storage.");
            }

            Trace.TraceInformation($"[Incidents]: Updating severity of incident with ID {clone.Id} with ETag {etag} to {newSeverity} in cloud storage.");
            if (!await _cloudStorageService.Update(
                clone.Id.ToString(),
                Incidents.IncidentSerializer.Serialize(clone),
                etag
            ))
            {
                Trace.TraceError($"[Incidents]: Failed to update severity of incident with ID {clone.Id} with ETag {etag} to {newSeverity} in cloud storage.");
                throw new InvalidOperationException($"Failed to update severity of incident with ID {clone.Id} with ETag {etag} to {newSeverity} in cloud storage.");
            }

            incident.UpdateSeverity(newSeverity, _currentUserId);

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to update severity of incident with ID {clone.Id} to {newSeverity} in cloud storage.");
            throw new InvalidOperationException($"Failed to update severity of incident with ID {clone.Id} to {newSeverity} in cloud storage: ", ex);
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

        clone.UpdateLocation(location, _currentUserId);

        try
        {
            Trace.TraceInformation($"[Incidents]: Reading ETag for incident with ID {clone.Id} in cloud storage.");
            (bool success, string etag) = await _cloudStorageService.ReadEtag(clone.Id.ToString());

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to read ETag for incident with ID {clone.Id} in cloud storage.");
                throw new InvalidOperationException("Failed to read ETag from cloud storage.");
            }

            Trace.TraceInformation($"[Incidents]: Updating location of incident with ID {clone.Id} with ETag {etag} to {location} in cloud storage.");
            success = await _cloudStorageService.Update(
                clone.Id.ToString(),
                Incidents.IncidentSerializer.Serialize(clone),
                etag
           );

            if (!success)
            {
                Trace.TraceError($"[Incidents]: Failed to update location of incident with ID {clone.Id} with ETag {etag} to {location} in cloud storage.");
                throw new InvalidOperationException($"Failed to update location of incident with ID {clone.Id} to {location} in cloud storage.");
            }

            incident.UpdateLocation(location, _currentUserId);

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to update location of incident with ID {clone.Id} to {location} in cloud storage.");
            throw new InvalidOperationException($"Failed to update location of incident with ID {clone.Id} to {location} in cloud storage: ", ex);
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

        if (!Incident.AddAdjacentIncident(incident1Clone, incident2Clone, _currentUserId))
        {
            return false;
        }

        try
        {
            Trace.TraceInformation($"[Incidents]: Reading ETags for incidents with IDs {incident1Clone.Id} and {incident2Clone.Id} in cloud storage.");
            (bool success, Dictionary<string, string> etags) = await _cloudStorageService.ReadEtags([incident1Clone.Id.ToString(), incident2Clone.Id.ToString()]);

            if (!success)
            {
                Trace.TraceInformation($"[Incidents]: Failed to read ETags for incidents with IDs {incident1Clone.Id} and {incident2Clone.Id} in cloud storage.");
                throw new InvalidOperationException($"Failed to read ETags  for incidents with IDs {incident1Clone.Id} and {incident2Clone.Id} in cloud storage.");
            }

            Dictionary<string, byte[]> itemsToUpdate = new Dictionary<string, byte[]>
            {
                { incident1Clone.Id.ToString(), Incidents.IncidentSerializer.Serialize(incident1Clone) },
                { incident2Clone.Id.ToString(), Incidents.IncidentSerializer.Serialize(incident2Clone) }
            };

            Trace.TraceInformation($"[Incidents]: Updating incidents with IDs {incident1Clone.Id} and {incident2Clone.Id} in cloud storage.");
            if (!await _cloudStorageService.UpdateAll(itemsToUpdate, etags))
            {
                Trace.TraceError($"[Incidents]: Failed to update incidents with IDs {incident1Clone.Id} and {incident2Clone.Id} in cloud storage.");
                throw new InvalidOperationException("Failed to update Incident to cloud storage.");
            }

            Incident.AddAdjacentIncident(incident1, incident2, _currentUserId);

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Incidents]: Failed to correlate incidents with IDs {incident1.Id} and {incident2.Id} in cloud storage");
            throw new InvalidOperationException($"Failed to correlate incidents with IDs {incident1.Id} and {incident2.Id} in cloud storage: ", ex);
        }
    }
}
