using System.Runtime.CompilerServices;
using Incidents;

namespace Location.Test.Fakes;

/// <summary>
/// Simple fake incident controller.
/// </summary>
internal sealed class FakeIncidentController : IIncidentController
{
    /// <summary>
    /// The list that GetAllIncidents returns.
    /// </summary>
    public List<Incident> Incidents { get; set; } = [];

    /// <summary>
    /// The incident that GetIncidentById returns.
    /// </summary>
    public Incident? IncidentById { get; set; }

    /// <summary>
    /// The filter passed to the last GetAllIncidents call.
    /// </summary>
    public IncidentFilter? LastFilter { get; private set; }

    /// <summary>
    /// Makes an empty incident for tests.
    /// </summary>
    /// <returns>An empty incident.</returns>
    public static Incident MakeIncident()
    {
        return (Incident)RuntimeHelpers.GetUninitializedObject(typeof(Incident));
    }
    public Task<List<Incident>> GetAllIncidents(IncidentFilter? filter = null)
    {
        LastFilter = filter;
        return Task.FromResult(Incidents);
    }
    public Task<Incident> GetIncidentById(Guid id)
    {
        return Task.FromResult(IncidentById ?? throw new KeyNotFoundException($"No incident with id {id}."));
    }
    public Task<Incident> CreateIncident(CreateIncidentRequest request)
    {
        throw new NotSupportedException();
    }
    public Task<List<Incident>> GetCorrelatedIncidents(Guid id)
    {
        throw new NotSupportedException();
    }
    public Task<bool> AddEvidence(Incident incident, List<(IncidentEvidence, byte[])> evidence)
    {
        throw new NotSupportedException();
    }
    public Task<bool> UpdateStatus(Incident incident, IncidentStatus newStatus)
    {
        throw new NotSupportedException();
    }
    public Task<bool> UpdateCategory(Incident incident, IncidentCategory newCategory)
    {
        throw new NotSupportedException();
    }
    public Task<bool> UpdateSeverity(Incident incident, IncidentSeverity newSeverity)
    {
        throw new NotSupportedException();
    }
    public Task<bool> UpdateLocation(Incident incident, IncidentLocation location)
    {
        throw new NotSupportedException();
    }
    public Task<bool> CorrelateIncident(Incident incident1, Incident incident2)
    {
        throw new NotSupportedException();
    }
}
