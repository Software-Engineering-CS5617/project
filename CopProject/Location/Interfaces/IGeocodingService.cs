namespace LocationTracker.Interfaces;

public interface IGeocodingService
{
    Task<string?> GetAddressAsync(double latitude, double longitude);
    Task<(double Latitude, double Longitude)?> FindCoordinatesAsync(string address);
}
