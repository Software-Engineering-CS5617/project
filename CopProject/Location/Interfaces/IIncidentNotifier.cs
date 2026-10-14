namespace LocationTracker.Interfaces;

using LocationTracker.Models;

public interface IIncidentNotifier
{
    Task NotifyIncidentCreatedAsync(Incident incident);
    Task NotifyIncidentUpdatedAsync(Incident incident);
}
