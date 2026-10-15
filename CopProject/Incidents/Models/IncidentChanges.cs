namespace Incidents;

public record CategoryChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

public record StatusChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

public record SeverityChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

public record LocationChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

public record IncidentCorrelationChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

public record IncidentEvidenceAdd(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;
