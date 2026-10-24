namespace Incident_module;

public record IncidentFilter(
    string? Title,
    IncidentStatus? Status,
    IncidentCategory? Category,
    IncidentSeverity? Severity,
    DateTime? FromDateTime,
    DateTime? ToDateTime,
    Guid? CreatedBy,
    IncidentLocationFilter? Location
);
