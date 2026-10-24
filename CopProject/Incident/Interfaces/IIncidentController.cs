using System;
using System.Collections.Generic;

namespace Incident_module;

public interface IIncidentController
{
    Task<Incident> CreateIncident(CreateIncidentRequest request);

    Task<Incident> GetIncidentById(Guid id);

    Task<List<Incident>> GetAllIncidents(IncidentFilter? filter = null);

    Task<List<Incident>> GetCorrelatedIncidents(Guid id);

    Task<bool> AddEvidence(Incident incident, List<IncidentEvidence> evidence);

    Task<bool> UpdateStatus(Incident incident, IncidentStatus newStatus);

    Task<bool> UpdateCategory(Incident incident, IncidentCategory newCategory);

    Task<bool> UpdateSeverity(Incident incident, IncidentSeverity newSeverity);

    Task<bool> UpdateLocation(Incident incident, IncidentLocation location);

    Task<bool> CorrelateIncident(Incident incident1, Incident incident2);
}
