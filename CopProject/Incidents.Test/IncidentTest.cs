// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Incidents;

namespace Incidents.Test;

[TestClass]
public sealed class IncidentTest
{
    [TestMethod]
    public void CreateEvidence()
    {
        IIncidentController<string> controller = new IncidentController<string>();

        Incident<string> incident = controller.CreateIncident(new CreateIncidentRequest(
            "Test Incident",
            "This is a test incident.",
            Guid.NewGuid(),
            IncidentCategory.Accident,
            IncidentSeverity.High,
            new IncidentLocation(37.7749, -122.4194),
            []
        ));

        Console.WriteLine($"Incident ID: {incident.Id}");
    }
}
