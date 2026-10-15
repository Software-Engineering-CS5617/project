namespace Incidents;

public record IncidentLocation(
    double Latitude,
    double Longitude,
    string? Address = null
);

public record IncidentLocationFilter(
    double Latitude,
    double Longitude,
    string? Address,
    double? Radius
);
