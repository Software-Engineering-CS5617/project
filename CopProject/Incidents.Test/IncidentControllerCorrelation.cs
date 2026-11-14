// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using FileSynchroniser.Models;
using Incidents;
using Incidents.Test.Implementations;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers;
using Cloud.Persistence;

namespace Incidents.Test;


/// <summary>
/// Test class for correlation requests to IncidentController
/// </summary>
[TestClass]
public sealed class IncidentControllerCorrelationTest
{
    private IncidentController _incidentController = null!;

    // Represents existing incidents persisted in the cloud
    private readonly List<Incident> _existingIncidents = [
        new Incident(
            title: "Test Incident 1",
            description: "This is a test incident 1.",
            createdBy: string.Empty,
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidence: [
                new IncidentEvidence(
                    Id: Guid.NewGuid(),
                    Description: "Test Evidence 1",
                    FilePath: "path/to/evidence1.jpg",
                    UploadTime: DateTime.Now,
                    UploadedBy: string.Empty
                ),
                new IncidentEvidence(
                    Id: Guid.NewGuid(),
                    Description: "Test Evidence 2",
                    FilePath: "path/to/evidence2.jpg",
                    UploadTime: DateTime.Now,
                    UploadedBy: string.Empty
                )
            ]
        ),
        new Incident(
            title: "Test Incident 2",
            description: "This is a test incident 2.",
            createdBy: string.Empty,
            category: IncidentCategory.DrunkDriving,
            severity: IncidentSeverity.High,
            location: new IncidentLocation(Latitude: 1.0, Longitude: 1.0)
        ),
        new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident 3",
            description: "This is a test incident 3.",
            createdBy: string.Empty,
            status: IncidentStatus.Resolved,
            category: IncidentCategory.Theft,
            severity: IncidentSeverity.Low,
            location: new IncidentLocation(Latitude: 3.0, Longitude: 3.0),
            creationTime: DateTime.Today.AddDays(-2),
            updateTime: DateTime.Today.AddDays(-1),
            changes: [
                    new CategoryChange(
                        Description: "Changed Category from In progess to resolved",
                        ChangedTime: DateTime.Today.AddDays(-1),
                        ChangedBy:string.Empty
                    )
                ],
            evidence: [],
            edges: []
        ),
        new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident 4",
            description: "This is a test incident 4.",
            createdBy: string.Empty,
            status: IncidentStatus.Resolved,
            category: IncidentCategory.DrunkDriving,
            severity: IncidentSeverity.High,
            location: new IncidentLocation(Latitude: 4.0, Longitude: 4.0),
            creationTime: DateTime.Today.AddDays(-2),
            updateTime: DateTime.Today.AddDays(-1),
            changes: [
                    new CategoryChange(
                        Description: "Changed Category from In progess to resolved",
                        ChangedTime: DateTime.Today.AddDays(-1),
                        ChangedBy:string.Empty
                    )
                ],
            evidence: [],
            edges: []
        )
    ];

    // Represents an incident that is not persisted in the cloud
    private readonly Incident _invalidIncident = new Incident(
            id: Guid.NewGuid(),
            title: "Invalid Incident",
            description: "This is an invalid Incident",
            createdBy: string.Empty,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            status: IncidentStatus.InProgress,
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Critical,
            creationTime: DateTime.Now,
            updateTime: DateTime.Now,
            evidence: [],
            changes: [],
            edges: []
          );

    /// <summary>
    /// Initialiazes IncidentController
    /// </summary>
    [TestInitialize]
    public async Task Setup()
    {
        await SetupIncidentController();
    }

    /// <summary>
    /// Initialiazes IncidentController with CloudService and FileSyncronizer
    /// </summary>
    /// <returns></returns>
    private async Task SetupIncidentController()
    {
        // Mimicking persistent cloud storage by adding Incidents before IncidentController is created
        IncidentCloudStorageService cloudStorageService = new IncidentCloudStorageService();
        await SetupIncidentCloudStorageService(cloudStorageService);

        IFileOperations fileOperationsService = new IncidentFileSynchroniser();
        _incidentController = await IncidentController.CreateAsync(cloudStorageService, fileOperationsService);
    }

    /// <summary>
    /// Initializes CloudStorageService with its persisted Incidents to mimick a cloud database
    /// </summary>
    /// <param name="cloudStorageService"></param>
    /// <returns></returns>
    private async Task SetupIncidentCloudStorageService(IncidentCloudStorageService cloudStorageService)
    {
        Dictionary<string, byte[]> payload = new();
        _existingIncidents.ForEach(i => {
            payload.Add(i.Id.ToString(), IncidentSerializer.Serialize(i));
        });
        await cloudStorageService.CreateAll(payload);
    }


    /// <summary>
    /// IncidentController throws when null incident is passed as argument
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task CorrelateIncident_NullIncident_ThrowArgumentNullException()
    {
        Incident incident = _existingIncidents[0];
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.CorrelateIncident(null!, incident);
        });
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.CorrelateIncident(incident, null!);
        });
    }

    /// <summary>
    /// Checks whether correlating same Incident returns false
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task CorrelateIncident_SameIncidents_ReturnsFalse()
    {
        Incident incident = _existingIncidents[0];
        bool result = await _incidentController.CorrelateIncident(incident, incident);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Checking whether correlating valid Incident returns true
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task CorrelateIncident_ValidIncidents_ReturnsTrue()
    {
        Incident incident1 = _existingIncidents[0];
        Incident incident2 = _existingIncidents[1];
        bool result = await _incidentController.CorrelateIncident(incident1, incident2);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Checking whether correlating valid Incident returns true
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task CorrelateIncident_ResolvedIncidents_ThrowsInvalidOperationException()
    {
        Incident incident1 = _existingIncidents[2];
        Incident incident2 = _existingIncidents[3];
        Assert.AreEqual(IncidentStatus.Resolved, incident1.Status);
        Assert.AreEqual(IncidentStatus.Resolved, incident2.Status);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => {
            await _incidentController.CorrelateIncident(incident1, incident2);
        });
    }

    /// <summary>
    /// Updating Severity of a Resolved Incident throws InvalidOperationException
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateSeverity_ResolvedIncident_ThrowsInvalidOperationException()
    {
        Incident incident = _existingIncidents[2];
        Assert.AreEqual(IncidentStatus.Resolved, incident.Status);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => {
            await _incidentController.UpdateSeverity(incident, IncidentSeverity.High);
        });
    }

    /// <summary>
    /// Updating Location of a Resolved Incident throws InvalidOperationException
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateLocation_ResolvedIncident_ThrowsInvalidOperationException()
    {
        Incident incident = _existingIncidents[2];
        Assert.AreEqual(IncidentStatus.Resolved, incident.Status);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => {
            await _incidentController.UpdateLocation(incident, new IncidentLocation(Latitude: 0.0, Longitude: 0.0));
        });
    }

    /// <summary>
    /// Updating Category of a Resolved Incident throws InvalidOperationException
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateCategory_ResolvedIncident_ThrowsInvalidOperationException()
    {
        Incident incident = _existingIncidents[2];
        Assert.AreEqual(IncidentStatus.Resolved, incident.Status);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => {
            await _incidentController.UpdateCategory(incident, IncidentCategory.DrunkDriving);
        });
    }
}
