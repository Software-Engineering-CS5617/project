namespace LocationTracker.Models;

public static class Coordinates
{
    public static bool IsValid(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }
}

