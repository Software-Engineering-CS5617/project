namespace Incidents;

public class CategoryChange : IChange<IncidentCategory>
{
    public IncidentCategory OldValue { get; }
    public IncidentCategory NewValue { get; }
    public DateTime UploadTime { get; }
    public Guid UploadedBy { get; }

    public CategoryChange(
        IncidentCategory oldValue,
        IncidentCategory newValue,
        DateTime uploadTime,
        Guid uploadedBy
    )
    {
        OldValue = oldValue;
        NewValue = newValue;
        UploadTime = uploadTime;
        UploadedBy = uploadedBy;
    }
}

public class StatusChange : IChange<IncidentStatus>
{
    public IncidentStatus OldValue { get; }
    public IncidentStatus NewValue { get; }
    public DateTime UploadTime { get; }
    public Guid UploadedBy { get; }

    public StatusChange(
        IncidentStatus oldValue,
        IncidentStatus newValue,
        DateTime uploadTime,
        Guid uploadedBy
    )
    {
        OldValue = oldValue;
        NewValue = newValue;
        UploadTime = uploadTime;
        UploadedBy = uploadedBy;
    }
}


public class SeverityChange : IChange<IncidentSeverity>
{
    public IncidentSeverity OldValue { get; }
    public IncidentSeverity NewValue { get; }
    public DateTime UploadTime { get; }
    public Guid UploadedBy { get; }
    public SeverityChange(
        IncidentSeverity oldValue,
        IncidentSeverity newValue,
        DateTime uploadTime,
        Guid uploadedBy
    )
    {
        OldValue = oldValue;
        NewValue = newValue;
        UploadTime = uploadTime;
        UploadedBy = uploadedBy;
    }
}
