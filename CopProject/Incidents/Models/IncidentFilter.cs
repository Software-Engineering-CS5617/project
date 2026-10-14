
namespace Incidents;


public class IncidentFilter
{
    public string? Title { get; set; } /* Regex applied on title of incident*/
    public IncidentStatus? Status { get; set; }
    public IncidentCategory? Category { get; set; }
    public IncidentSeverity? Severity { get; set; }
    public DateTime? FromDateTime { get; set; }
    public DateTime? ToDateTime { get; set; }
    public Guid? CreatedBy { get; set; }
    public IncidentLocationFilter? Location { get; set; }
}
