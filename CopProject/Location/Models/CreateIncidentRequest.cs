namespace LocationTracker.Models;

public class CreateIncidentRequest
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public IncidentSeverity Severity { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
