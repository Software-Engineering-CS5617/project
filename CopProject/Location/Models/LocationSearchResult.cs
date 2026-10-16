namespace LocationTracker.Models;

public record LocationSearchResult(
    double Latitude,
    double Longitude,
    string? Address
);
