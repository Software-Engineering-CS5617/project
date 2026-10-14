namespace Incidents;

public record IncidentLocation(
    double Latitude,
    double Longitude,
    string? Address
);

public record IncidentLocationFilter(
    double Latitude,
    double Longitude,
    string? Address,
    double? Radius
);
