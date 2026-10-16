using System;

namespace Incidents;

public class IncidentLocation
{
    public double Latitude { get; }
    public double Longitude { get; }
    public string? Address { get; }

    public IncidentLocation(
        double latitude,
        double longitude,
        string? address = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
    }

    public double DistanceToKm(double latitude, double longitude)
    {
        const double EarthRadiusKm = 6371.0;

        double lat1 = Latitude * Math.PI / 180.0;
        double lat2 = latitude * Math.PI / 180.0;

        double deltaLat = (latitude - Latitude) * Math.PI / 180.0;
        double deltaLon = (longitude - Longitude) * Math.PI / 180.0;

        double a =
            Math.Pow(Math.Sin(deltaLat / 2), 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Pow(Math.Sin(deltaLon / 2), 2);

        a = Math.Clamp(a, 0.0, 1.0);

        return EarthRadiusKm * 2 * Math.Asin(Math.Sqrt(a));
    }
}

public class IncidentLocationFilter
{
    public double Latitude { get; }
    public double Longitude { get; }
    public string? Address { get; }
    public double? Radius { get; }

    public IncidentLocationFilter(
        double latitude,
        double longitude,
        string? address = null,
        double? radius = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
        Radius = radius;
    }

    public static bool Matches(IncidentLocation location, IncidentLocationFilter filter)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(filter);

        if (!string.IsNullOrWhiteSpace(filter.Address))
        {
            if (string.IsNullOrWhiteSpace(location.Address) ||
                !location.Address.Contains(
                    filter.Address,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (filter.Radius is null)
        {
            return location.Latitude == filter.Latitude && location.Longitude == filter.Longitude;
        }

        return location.DistanceToKm(filter.Latitude, filter.Longitude) <= filter.Radius.Value;
    }
}
