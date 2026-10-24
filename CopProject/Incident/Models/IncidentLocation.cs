namespace Incident_module;

public record IncidentLocation(
    double Latitude,
    double Longitude,
    string? Address = null
);

public record IncidentLocationFilter(
    double Latitude,
    double Longitude,
    string? Address = null,
    double? Radius = null
);
