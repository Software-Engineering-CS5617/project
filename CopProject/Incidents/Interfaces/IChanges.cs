namespace Incidents;

internal interface IChange
{
    string Description { get; }
    DateTime ChangedTime { get; }
    Guid ChangedBy { get; }
}
