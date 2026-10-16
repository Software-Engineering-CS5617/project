using Incidents;

namespace LocationTracker.Interfaces;

public interface IMapService
{
    List<Incident> GetIncidentsForMap(IncidentFilter? filter = null);
    Incident GetIncidentById(Guid id);
}
