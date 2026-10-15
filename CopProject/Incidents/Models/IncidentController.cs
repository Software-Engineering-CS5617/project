// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Incidents;

public class IncidentController<T> : IIncidentController<T>
{
    public Incident<T> CreateIncident(CreateIncidentRequest request)
    {
        return new Incident<T>(
            request.Title,
            request.Description,
            request.CreatedBy,
            request.Category,
            request.Severity,
            request.Location
        );
    }

    public Incident<T> GetIncidentById(Guid id)
    {
        throw new NotImplementedException();
    }

    public List<Incident<T>> GetAllIncidents(IncidentFilter filter)
    {
        throw new NotImplementedException();
    }

    public List<Incident<T>> GetCorrelatedIncidents(Guid id)
    {
        throw new NotImplementedException();
    }

    public bool AddEvidence(Guid id, List<IncidentEvidence> evidence)
    {
        throw new NotImplementedException();
    }

    public bool UpdateStatus(Guid id, IncidentStatus newStatus)
    {
        throw new NotImplementedException();
    }

    public bool UpdateCategory(Guid id, IncidentCategory newCategory)
    {
        throw new NotImplementedException();
    }

    public bool UpdateSeverity(Guid id, IncidentSeverity newSeverity)
    {
        throw new NotImplementedException();
    }

    public bool UpdateLocation(Guid id, IncidentLocation location)
    {
        throw new NotImplementedException();
    }

    public bool CorrelateIncident(Guid id1, Guid id2)
    {
        throw new NotImplementedException();
    }
}
