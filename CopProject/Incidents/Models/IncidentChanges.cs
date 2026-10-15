namespace Incidents;

public class CategoryChange : IChange
{
    public string Description { get; }
    public DateTime ChangedTime { get; }
    public Guid ChangedBy { get; }

    public CategoryChange(
        string description,
        DateTime changedTime,
        Guid changedBy
    )
    {
        Description = description;
        ChangedTime = changedTime;
        ChangedBy = changedBy;
    }
}

public class StatusChange : IChange
{
    public string Description { get; }
    public DateTime ChangedTime { get; }
    public Guid ChangedBy { get; }

    public StatusChange(
        string description,
        DateTime changedTime,
        Guid changedBy
    )
    {
        Description = description;
        ChangedTime = changedTime;
        ChangedBy = changedBy;
    }
}


public class SeverityChange : IChange
{
    public string Description { get; }
    public DateTime ChangedTime { get; }
    public Guid ChangedBy { get; }

    public SeverityChange(
        string description,
        DateTime changedTime,
        Guid changedBy
    )
    {
        Description = description;
        ChangedTime = changedTime;
        ChangedBy = changedBy;
    }
}

public class LocationChange : IChange
{
    public string Description { get; }
    public DateTime ChangedTime { get; }
    public Guid ChangedBy { get; }

    public LocationChange(
        string description,
        DateTime changedTime,
        Guid changedBy
    )
    {
        Description = description;
        ChangedTime = changedTime;
        ChangedBy = changedBy;
    }
}

public class IncidentCorrelationChange : IChange
{
    public string Description { get; }
    public DateTime ChangedTime { get; }
    public Guid ChangedBy { get; }

    public IncidentCorrelationChange(
        string description,
        DateTime changedTime,
        Guid changedBy
    )
    {
        Description = description;
        ChangedTime = changedTime;
        ChangedBy = changedBy;
    }
}

public class IncidentEvidenceAdd : IChange
{
    public string Description { get; }
    public DateTime ChangedTime { get; }
    public Guid ChangedBy { get; }

    public IncidentEvidenceAdd(
        string description,
        DateTime changedTime,
        Guid changedBy
    )
    {
        Description = description;
        ChangedTime = changedTime;
        ChangedBy = changedBy;
    }
}
