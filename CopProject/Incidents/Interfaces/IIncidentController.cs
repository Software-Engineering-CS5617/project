using System;
using System.Collections.Generic;

namespace Incidents;

public interface IIncidentController
{
    Incident CreateIncident(CreateIncidentRequest request);

    Incident GetIncidentById(Guid id);

    List<Incident> GetAllIncidents(IncidentFilter? filter = null);

    List<Incident> GetCorrelatedIncidents(Guid id);

    bool AddEvidence(Incident incident, List<IncidentEvidence> evidence);

    bool UpdateStatus(Incident incident, IncidentStatus newStatus);

    bool UpdateCategory(Incident incident, IncidentCategory newCategory);

    bool UpdateSeverity(Incident incident, IncidentSeverity newSeverity);

    bool UpdateLocation(Incident incident, IncidentLocation location);

    bool CorrelateIncident(Incident incident1, Incident incident2);
}
