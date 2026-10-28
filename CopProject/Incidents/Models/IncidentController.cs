using System;
using System.Collections.Generic;
using System.Text;

namespace Incidents;

public class IncidentController : IIncidentController
{
    private readonly List<Incident> _incidents =
    [
        new Incident(
            "FLood in Palakkad",
            "Streets have flooded in IIT Palakkad.",
            Guid.Empty,
            IncidentCategory.Accident,
            IncidentSeverity.Medium,
            new IncidentLocation(10.8069, 76.7254, "IIT Palakkad Nila Campus")
        ),
        new Incident(
            "Theft reported",
            "A theft was reported near the stadium bus stand.",
            Guid.Empty,
            IncidentCategory.Theft,
            IncidentSeverity.High,
            new IncidentLocation(10.7739, 76.6626, "Stadium Bus Stand")
        ),
        new Incident(
            "Drunk driving",
            "A suspected drunk driver was reported on the railway station.",
            Guid.Empty,
            IncidentCategory.DrunkDriving,
            IncidentSeverity.Critical,
            new IncidentLocation(10.7993, 76.6376, "Palakkad Junction")
        )
    ];

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
        IEnumerable<Incident> incidents = _incidents;

        if (filter is null)
        {
            return incidents.ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            string title = filter.Title;
            incidents = incidents.Where(incident => incident.Title.Contains(title));
        }

        if (filter.Status is IncidentStatus status)
        {
            incidents = incidents.Where(incident => incident.Status == status);
        }

        if (filter.Category is IncidentCategory category)
        {
            incidents = incidents.Where(incident => incident.Category == category);
        }

        if (filter.Severity is IncidentSeverity severity)
        {
            incidents = incidents.Where(incident => incident.Severity == severity);
        }

        if (filter.FromDateTime is DateTime from)
        {
            incidents = incidents.Where(incident => incident.CreationTime >= from);
        }

        if (filter.ToDateTime is DateTime to)
        {
            incidents = incidents.Where(incident => incident.CreationTime <= to);
        }

        return incidents.ToList();
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
