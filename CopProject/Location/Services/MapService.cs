using Incidents;
using LocationTracker.Interfaces;
using LocationTracker.Models;

namespace LocationTracker.Services;

public class MapService : IMapService
{
    private readonly IIncidentController _incidents;
    private readonly IGeocodingService _geocoding;

    public MapService(IIncidentController incidents, IGeocodingService geocoding)
    {
        _incidents = incidents;
        _geocoding = geocoding;
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

    public List<Incident> GetNearbyIncidents(double latitude, double longitude, double radius)
    {
        if (!Coordinates.IsValid(latitude, longitude))
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "The coordinates are outside the valid range.");
        }

        if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "The radius must be a non-negative finite value.");
        }

        var location = new IncidentLocationFilter(latitude, longitude, Radius: radius);
        var filter = new IncidentFilter(null, null, null, null, null, null, null, location);
        return _incidents.GetAllIncidents(filter);
    }

    public async Task<IncidentLocation?> GetLocationForClickAsync(double latitude, double longitude)
    {
        if (!Coordinates.IsValid(latitude, longitude))
        {
            return null;
        }

        string? address = await _geocoding.GetAddressAsync(latitude, longitude);
        return new IncidentLocation(latitude, longitude, address);
    }

    public async Task<LocationSearchResult?> SearchAddressAsync(string address)
    {
        (double Latitude, double Longitude)? coordinates = await _geocoding.FindCoordinatesAsync(address);
        if (coordinates == null)
        {
            return null;
        }

        return new LocationSearchResult(coordinates.Value.Latitude, coordinates.Value.Longitude, address);
    }

}
