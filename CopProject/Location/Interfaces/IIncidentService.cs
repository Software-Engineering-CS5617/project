namespace LocationTracker.Interfaces;

using LocationTracker.Models;

public interface IIncidentService
{
    Task<Incident> CreateAsync(CreateIncidentRequest request);
    List<Incident> GetAllForMap(IncidentFilter filter);
    Incident? GetById(Guid id);
    Task<bool> UpdateStatusAsync(Guid id, IncidentStatus newStatus);
}
