using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Incidents;

public interface IIncidentService<T>
{
    Task<bool> CreateIncidentAsync(CreateIncidentRequest request);

    Task<Incident<T>?> GetIncidentByIdAsync(Guid id);

    Task<List<Incident<T>>> GetAllIncidentAsync(IncidentFilter filter);

    Task<List<Incident<T>>> GetCorrelatedIncidentsAsync(Guid id);

    Task<bool> AddEvidenceAsync(
        Guid id,
        List<IncidentEvidence> evidence
    );

    Task<bool> UpdateStatusAsync(
        Guid id,
        IncidentStatus newStatus
    );

    Task<bool> UpdateCategoryAsync(
        Guid id,
        IncidentCategory newCategory
    );

    Task<bool> UpdateSeverityAsync(
        Guid id,
        IncidentSeverity newSeverity
    );

    Task<bool> UpdateLocationAsync(
        Guid id,
        IncidentLocation newLocation
    );

    Task<bool> CorrelateIncidentAsync(Guid id1, Guid id2);
}
