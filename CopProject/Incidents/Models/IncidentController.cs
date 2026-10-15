using System;
using System.Collections.Generic;
using System.Text;

namespace Incidents;

public class IncidentController : IIncidentController
{
    // IFileSyncService and ICloudService are to be used for file synchronization and cloud storage.
    public Incident CreateIncident(CreateIncidentRequest request)
    {
        throw new NotImplementedException();
    }

    public Incident GetIncidentById(Guid id)
    {
        throw new NotImplementedException();
    }

    public List<Incident> GetAllIncidents(IncidentFilter? filter = null)
    {
        throw new NotImplementedException();
    }

    public List<Incident> GetCorrelatedIncidents(Guid id)
    {
        throw new NotImplementedException();
    }

    public bool AddEvidence(Incident incident, List<IncidentEvidence> evidence)
    {
        throw new NotImplementedException();
    }

    public bool UpdateStatus(Incident incident, IncidentStatus newStatus)
    {
        throw new NotImplementedException();
    }

    public bool UpdateCategory(Incident incident, IncidentCategory newCategory)
    {
        throw new NotImplementedException();
    }

    public bool UpdateSeverity(Incident incident, IncidentSeverity newSeverity)
    {
        throw new NotImplementedException();
    }

    public bool UpdateLocation(Incident incident, IncidentLocation location)
    {
        throw new NotImplementedException();
    }

    public bool CorrelateIncident(Incident incident1, Incident incident2)
    {
        throw new NotImplementedException();
    }
}
