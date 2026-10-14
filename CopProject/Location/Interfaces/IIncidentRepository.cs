namespace LocationTracker.Interfaces;

using LocationTracker.Models;

public interface IIncidentRepository
{
    List<Incident> GetAll(IncidentFilter filter);
    Incident? GetById(Guid id);
    void Add(Incident incident);
    bool UpdateStatus(Guid id, IncidentStatus newStatus);
}
