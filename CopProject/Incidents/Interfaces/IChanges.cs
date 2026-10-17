namespace Incidents;

public interface IChange
{
    string Description { get; }
    DateTime ChangedTime { get; }
    Guid ChangedBy { get; }
}
