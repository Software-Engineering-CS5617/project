using System.Globalization;
using System.Text.Json;
using LocationTracker.Interfaces;
using LocationTracker.Models;

namespace LocationTracker.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    public NominatimGeocodingService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("CopProject-LocationTracker/1.0");
    }
    public async Task<string?> GetAddressAsync(double latitude, double longitude)
    {
        if (!Coordinates.IsValid(latitude, longitude))
        {
            return null;
        }

        string lat = latitude.ToString(CultureInfo.InvariantCulture);
        string lon = longitude.ToString(CultureInfo.InvariantCulture);
        string json = await _http.GetStringAsync($"reverse?format=jsonv2&lat={lat}&lon={lon}");

        using JsonDocument doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("display_name", out JsonElement name))
        {
            return name.GetString();
        }
        return null;
    }

    public async Task<(double Latitude, double Longitude)?> FindCoordinatesAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        string query = Uri.EscapeDataString(address);
        string json = await _http.GetStringAsync($"search?format=jsonv2&limit=1&q={query}");

        using JsonDocument doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement first = doc.RootElement[0];
        double latitude = double.Parse(first.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
        double longitude = double.Parse(first.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);

        if (!Coordinates.IsValid(latitude, longitude))
        {
            return null;
        }
        return (latitude, longitude);
    }
}
