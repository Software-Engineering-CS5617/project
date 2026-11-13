namespace Incidents;

public record IncidentFilter(
    string? Title = null,
    IncidentStatus? Status = null,
    IncidentCategory? Category = null,
    IncidentSeverity? Severity = null,
    DateTime? FromDateTime = null,
    DateTime? ToDateTime = null,
    string? CreatedBy = null,
    IncidentLocationFilter? Location = null
);
