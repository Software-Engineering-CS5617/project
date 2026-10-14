namespace LocationTracker.Models;

public class IncidentFilter
{
    public IncidentStatus? Status { get; set; }
    public string? Category { get; set; }
    public DateTime? Since { get; set; }
}
