using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

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

    public Task<List<Incident>> GetCorrelatedIncidents(Guid id)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
