namespace LocationTracker.Models;

public class Incident
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = "";
    public DateTime ReportedAt { get; set; }
}
