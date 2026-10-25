using Incidents;
using LocationTracker.Models;
namespace LocationTracker.Interfaces;

public interface IMapService
{
    List<Incident> GetIncidentsForMap(IncidentFilter? filter = null);
    Incident GetIncidentById(Guid id);
    List<Incident> GetRecentIncidents(TimeSpan window);
    Task<IncidentLocation?> GetLocationForClickAsync(double latitude, double longitude);
    Task<LocationSearchResult?> SearchAddressAsync(string address);
}
