namespace LocationTracker.Models;

public sealed record MapIncident(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Severity,
    string Status,
    double Latitude,
    double Longitude,
    string? Address
);
