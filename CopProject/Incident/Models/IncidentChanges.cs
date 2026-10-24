namespace Incident_module;

internal record CategoryChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

internal record StatusChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

internal record SeverityChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

internal record LocationChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

internal record IncidentCorrelationChange(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;

internal record IncidentEvidenceAdd(
    string Description,
    DateTime ChangedTime,
    Guid ChangedBy
) : IChange;
