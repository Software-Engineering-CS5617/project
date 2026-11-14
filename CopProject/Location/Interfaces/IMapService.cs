using Incidents;
using LocationTracker.Models;
namespace LocationTracker.Interfaces;

public interface IMapService
{
    Task<List<Incident>> GetIncidentsForMapAsync(IncidentFilter? filter = null);
    Task<Incident> GetIncidentByIdAsync(Guid id);
    Task<List<Incident>> GetRecentIncidentsAsync(TimeSpan window);
    Task<List<Incident>> GetNearbyIncidentsAsync(double latitude, double longitude, double radius);
    Task<IncidentLocation?> GetLocationForClickAsync(double latitude, double longitude);
    Task<LocationSearchResult?> SearchAddressAsync(string address);
}
