namespace Incidents;

public class IncidentCorrelation
{
    public Guid Id { get; }
    public Guid IncidentId1 { get; }
    public Guid IncidentId2 { get; }
    public DateTime CreationTime { get; }
    public Guid CreatedBy { get; }

    public IncidentCorrelation(
        Guid incidentId1,
        Guid incidentId2,
        DateTime creationTime,
        Guid createdBy
    )
    {
        Id = Guid.NewGuid();
        IncidentId1 = incidentId1;
        IncidentId2 = incidentId2;
        CreationTime = creationTime;
        CreatedBy = createdBy;
    }
}
