using Incidents;
using LocationTracker.Interfaces;

namespace LocationTracker.Services;

public class MapService : IMapService
{
    private readonly IIncidentController _incidents;

    public MapService(IIncidentController incidents)
    {
        _incidents = incidents;
    }

    public List<Incident> GetIncidentsForMap(IncidentFilter? filter = null)
    {
        return _incidents.GetAllIncidents(filter);
    }

    public Incident GetIncidentById(Guid id)
    {
        return _incidents.GetIncidentById(id);
    }

    public List<Incident> GetRecentIncidents(TimeSpan window)
    {
        var filter = new IncidentFilter(null, null, null, null, DateTime.Now - window, null, null, null);
        return _incidents.GetAllIncidents(filter);
    }

}

