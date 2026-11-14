// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Incidents;

namespace Incidents;

/// <summary>
/// Provides a request model for creating a new Incident.
/// </summary>
public class CreateIncidentRequest
{
    public string Title { get; }
    public string Description { get; }

    public string CreatedBy { get; } = "";
    public IncidentStatus Status { get; }
    public IncidentCategory Category { get; }
    public IncidentSeverity Severity { get; }
    public IncidentLocation Location { get; }

    private readonly List<(IncidentEvidence, byte[])> _evidences;
    public IReadOnlyList<(IncidentEvidence, byte[])> Evidences => _evidences.AsReadOnly();

    public CreateIncidentRequest(
        string title,
        string description,
        string createdBy,
        IncidentCategory category,
        IncidentSeverity severity,
        IncidentLocation location,
        List<(IncidentEvidence, byte[])> evidences
    )
    {
        Title = title;
        Description = description;
        CreatedBy = createdBy;
        Status = IncidentStatus.Reported;
        Category = category;
        Severity = severity;
        Location = location;
        _evidences = evidences.Select(e => (e.Item1, (byte[])e.Item2.Clone())).ToList(); // deep copy
    }
}
