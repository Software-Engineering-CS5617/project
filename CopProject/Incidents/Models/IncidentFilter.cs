namespace Incidents;


public record IncidentFilter(
    string? Title, /* Regex applied on title of Incident */
    IncidentStatus? Status,
    IncidentCategory? Category,
    IncidentSeverity? Severity,
    DateTime? FromDateTime,
    DateTime? ToDateTime,
    Guid? CreatedBy,
    IncidentLocationFilter? Location
);
