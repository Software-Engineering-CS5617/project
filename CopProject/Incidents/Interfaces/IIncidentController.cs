using System;
using System.Collections.Generic;

namespace Incidents;

public interface IIncidentController<T>
{
    Incident<T> CreateIncident(CreateIncidentRequest request);

    Incident<T> GetIncidentById(Guid id);

    List<Incident<T>> GetAllIncidents(IncidentFilter filter);

    List<Incident<T>> GetCorrelatedIncidents(Guid id);

    bool AddEvidence(Guid id, List<IncidentEvidence> evidence);

    bool UpdateStatus(Guid id, IncidentStatus newStatus);

    bool UpdateCategory(Guid id, IncidentCategory newCategory);

    bool UpdateSeverity(Guid id, IncidentSeverity newSeverity);

    bool UpdateLocation(Guid id, IncidentLocation location);

    bool CorrelateIncident(Guid id1, Guid id2);
}
