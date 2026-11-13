// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security.Policy;
using FileSynchroniser.Models;
using Incidents;
using Incidents.Test.Implementations;
using Cloud.Persistence;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;


namespace Incidents.Test;

[TestClass]
public sealed class IncidentControllerTest
{
    private IncidentController _incidentController = null!;

    private static readonly object?[] s_filters =
    [
        // No filter
        null,

        // Individual filters
        new IncidentFilter(Title: "Test Incident 1"),
        new IncidentFilter(Status: IncidentStatus.InProgress),
        new IncidentFilter(Category: IncidentCategory.Accident),
        new IncidentFilter(Severity: IncidentSeverity.High),
        new IncidentFilter(FromDateTime: new DateTime(2026, 1, 1)),
        new IncidentFilter(ToDateTime: new DateTime(2026, 12, 31)),
        new IncidentFilter(CreatedBy: "person1"),

        // Multiple filters
        new IncidentFilter(
            Category: IncidentCategory.Accident,
            Severity: IncidentSeverity.High,
            Status: IncidentStatus.Reported
        ),

        // Date range
        new IncidentFilter(
            FromDateTime: new DateTime(2026, 1, 1),
            ToDateTime: new DateTime(2026, 12, 31)
        ),
        
        // All filters
        new IncidentFilter(
            Title: "Test Incident",
            Status: IncidentStatus.InProgress,
            Category: IncidentCategory.Accident,
            Severity: IncidentSeverity.High,
            FromDateTime: new DateTime(2026, 1, 1, 0, 0, 0),
            ToDateTime: new DateTime(2026, 12, 31, 23, 59, 59),
            CreatedBy: "person1",
            Location: new IncidentLocationFilter(
                Latitude: 10.80,
                Longitude: 76.72,
                Address: "IIT Palakkad, Kerala, India",
                Radius: 10.0
            )
        )
    ];

    public static IEnumerable<object?[]> IncidentFilters =>
        s_filters.Select(filter => new object?[] { filter });

    /// <summary>
    /// Initialiazes IncidentController
    /// </summary>
    [TestInitialize]
    public async Task Setup()
    {
        await SetupIncidentController();
        SetupMockIncidents();
    }

    private async Task SetupIncidentController()
    {
        IStorage cloudStorageService = new IncidentCloudStorageService();
        IFileOperations fileOperationsService = new IncidentFileSynchroniser();
        _incidentController = await IncidentController.CreateAsync(cloudStorageService, fileOperationsService);
    }

    /// <summary>
    /// Checks that creating an incident with a null argument throws an ArgumentNullException.
    /// </summary>
    [TestMethod]
    public async Task CreateNullIncident()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
          () => _incidentController.CreateIncident(null!));
    }

    [TestMethod]
    public async Task CreateIncident()
    {
        var incident = new CreateIncidentRequest(
            title: "Test Incident",
            description: "This is a test incident.",
            createdBy: "person1",
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<(IncidentEvidence Evidence, byte[] Content)>()
        );

        Incident createdIncident = await _incidentController.CreateIncident(incident);

        Assert.IsNotNull(createdIncident);
    }

    /// <summary>
    /// Get incident by id and check that the retrieved incident matches the created incident.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task GetIncidentById()
    {
        var incident = new CreateIncidentRequest(
            title: "Test Incident",
            description: "This is a test incident.",
            createdBy: "person1",
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<(IncidentEvidence Evidence, byte[] Content)>()
        );

        Incident createdIncident = await _incidentController.CreateIncident(incident);

        Incident retrievedIncident = await _incidentController.GetIncidentById(createdIncident.Id);

        Assert.IsNotNull(createdIncident);
        Assert.IsNotNull(retrievedIncident);
        Assert.AreEqual(createdIncident.Id, retrievedIncident.Id);
        Assert.AreEqual(incident.Title, retrievedIncident.Title);
        Assert.AreEqual(incident.Description, retrievedIncident.Description);
        Assert.AreEqual(incident.CreatedBy, retrievedIncident.CreatedBy);
        Assert.AreEqual(incident.Status, retrievedIncident.Status);
        Assert.AreEqual(incident.Category, retrievedIncident.Category);
        Assert.AreEqual(incident.Severity, retrievedIncident.Severity);
        Assert.AreEqual(incident.Location.Latitude, retrievedIncident.Location.Latitude);
        Assert.AreEqual(incident.Location.Longitude, retrievedIncident.Location.Longitude);
    }

    private List<Incident> _mockIncidents = new List<Incident>();

    /// <summary>
    /// Creates few incidents for testing
    /// </summary>
    /// <returns></returns>
    private void SetupMockIncidents()
    {
        _mockIncidents.AddRange(
            [
            new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident 1",
            description: "This is a test incident 1.",
            createdBy: "person1",
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            status: IncidentStatus.Reported,
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidence: [],
            changes: [],
            edges: []
            ),
       new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident 2",
            description: "This is a test incident 2.",
            createdBy: "person2",
            category: IncidentCategory.DrunkDriving,
            severity: IncidentSeverity.High,
            status: IncidentStatus.InProgress,
            location: new IncidentLocation(Latitude: 1.0, Longitude: 1.0),
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            evidence: [],
            changes: [],
            edges: []
         ),
       new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident 3",
            description: "This is a test incident 3.",
            createdBy: "person1",
            category: IncidentCategory.Theft,
            severity: IncidentSeverity.Low,
            status: IncidentStatus.InProgress,
            location: new IncidentLocation(Latitude: 3.0, Longitude: 3.0),
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            evidence: [],
            changes: [],
            edges: []
         )
        ]);
    }

    /// <summary>
    /// Get incident by id and check that the retrieved incident matches the created incident.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    [DynamicData(nameof(IncidentFilters))]
    public async Task GetAllIncidents(IncidentFilter filter)
    {
        List<Incident> incidents = await _incidentController.GetAllIncidents(filter);

        Assert.IsNotNull(incidents);
    }

    [TestMethod]
    public async Task AddEvidence_NullIncident()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _incidentController.AddEvidence(null!, []));
    }

    [TestMethod]
    public async Task AddEvidence_NullEvidence()
    {
        if (_mockIncidents.Count == 0)
        {
            Assert.Fail("No incidents created for testing.");
        }

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _incidentController.AddEvidence(_mockIncidents[0], null!));
    }

    [TestMethod]
    public async Task AddEvidence_ResolvedIncident()
    {
        Incident resolvedIncident = new Incident(
            id: Guid.NewGuid(),
            title: "Resolved Incident",
            description: "This incident is resolved.",
            createdBy: "person1",
            status: IncidentStatus.Resolved,
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidence: [],
            changes: [],
            edges: []
        );

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _incidentController.AddEvidence(resolvedIncident, []));
    }

    [TestMethod]
    public async Task AddEvidence_ETagNotFound()
    {
        Incident? incident = _mockIncidents.Find(i => i.Status != IncidentStatus.Resolved);

        if (incident == null)
        {
            Assert.Fail("No incidents created for testing.");
        }

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _incidentController.AddEvidence(incident, new List<(IncidentEvidence, byte[])> {
                (new IncidentEvidence(
                    Id: Guid.NewGuid(),
                    Description: "Test Evidence",
                    FilePath: "/path/to/test_image.jpg",
                    UploadTime: DateTime.UtcNow,
                    UploadedBy: "person1"
                    ), []
                )
            }));
    }

    [TestMethod]
    public async Task AddEvidence_ValidIncident()
    {
        Incident? incident = await _incidentController.CreateIncident(new CreateIncidentRequest(
            title: "Test Incident",
            description: "This is a test incident.",
            createdBy: "person1",
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<(IncidentEvidence Evidence, byte[] Content)>()
        ));

        if (incident == null)
        {
            Assert.Fail("No incidents created for testing.");
        }

        Assert.IsTrue(await _incidentController.AddEvidence(incident, new List<(IncidentEvidence, byte[])> {
            (new IncidentEvidence(
                Id: Guid.NewGuid(),
                Description: "Test Evidence",
                FilePath: "/path/to/test_image.jpg",
                UploadTime: DateTime.UtcNow,
                UploadedBy: "person1"
            ), [])
        }));
    }
}
