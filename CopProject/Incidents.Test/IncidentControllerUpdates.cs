using FileSynchroniser.Models;
using Incidents;
using Incidents.Test.Implementations;
using Cloud.Persistence;

namespace Incidents.Test;

/// <summary>
/// Test class for update requests to IncidentController
/// </summary>
[TestClass]
public sealed class IncidentControllerUpdatesTest
{
    private IncidentController _incidentController = null!;
    private readonly List<Incident> _mockIncidents = [];

    // An incident that does not even exist in the cloud
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
        await SetupMockIncidents();
    }

    /// <summary>
    /// Setups the Incident Controller with CloudService and FileSyncronizer
    /// </summary>
    /// <returns></returns>
    private async Task SetupIncidentController()
    {
        IStorage cloudStorageService = new IncidentCloudStorageService();
        IFileOperations fileOperationsService = new IncidentFileSynchroniser();
        _incidentController = await IncidentController.CreateAsync(cloudStorageService, fileOperationsService);
    }

    private readonly List<CreateIncidentRequest> _createIncidentRequests = [
        new CreateIncidentRequest(
            title: "Test Incident 1",
            description: "This is a test incident 1.",
            createdBy: string.Empty,
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: []
        ),
        new CreateIncidentRequest(
            title: "Test Incident 2",
            description: "This is a test incident 2.",
            createdBy: string.Empty,
            category: IncidentCategory.DrunkDriving,
            severity: IncidentSeverity.High,
            location: new IncidentLocation(Latitude: 1.0, Longitude: 1.0),
            evidences: []
        ),
        new CreateIncidentRequest(
            title: "Test Incident 3",
            description: "This is a test incident 3.",
            createdBy: string.Empty,
            category: IncidentCategory.Theft,
            severity: IncidentSeverity.Low,
            location: new IncidentLocation(Latitude: 3.0, Longitude: 3.0),
            evidences: []
        )
    ];

    /// <summary>
    /// Generate random number in the range [-min,+min]
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static double RandomDouble(double min, double max)
    {
        return Random.Shared.NextDouble() * (max - min) + min;
    }

    /// <summary>
    /// Creates few incidents for testing
    /// </summary>
    /// <returns></returns>
    private async Task SetupMockIncidents()
    {
        await Task.WhenAll(_createIncidentRequests.Select(async (request) => {
            Incident incident = await _incidentController.CreateIncident(request);
            _mockIncidents.Add(incident);
        }));
    }

    /// <summary>
    /// Controller throws when null inicdent passed as argument to UpdateCategory
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateCategory_NullIncident_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateCategory(null!, IncidentCategory.Theft);
        });
    }

    /// <summary>
    /// Controller throws when invalid category passed as argument to UpdateCategory
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateCategory_InvalidCategory_ThrowsArgumentOutOfRangeException()
    {
        Incident incident = _mockIncidents[0];
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => {
            await _incidentController.UpdateCategory(incident, (IncidentCategory)int.MaxValue!);
        });
    }

    /// <summary>
    /// Controller throws when invalid Incident passed as argument to UpdateCategory
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateCategory_InvalidIncident_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => {
            await _incidentController.UpdateCategory(_invalidIncident, IncidentCategory.Theft);
        });
    }

    /// <summary>
    /// Update location of incident which is valid but same location it is currently in. 
    /// todo(@shreesh): update this test when backend changes
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateCategory_ValidIncidentSameCategory_ReturnsTrue()
    {
        Incident incident = _mockIncidents[0];
        Assert.AreEqual(IncidentCategory.Accident, incident.Category);
        bool result = await _incidentController.UpdateCategory(incident, IncidentCategory.Accident);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Check whether a valid Incident is updated with a valid but different category
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateCategory_ValidIncidentDifferentCategory_ReturnsTrue()
    {
        Incident incident = _mockIncidents[0];
        Assert.AreNotEqual(IncidentCategory.DrunkDriving, incident.Category);
        bool result = await _incidentController.UpdateCategory(incident, IncidentCategory.DrunkDriving);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Controller throws when null inicdent passed as argument to UpdateLocation
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateLocation_NullIncident_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateLocation(null!,
                new IncidentLocation(
                    Latitude: RandomDouble(-90, 90),
                    Longitude: RandomDouble(-180, 180))
                );
        });
    }

    /// <summary>
    /// Controller throws when null location passed as argument to UpdateLocation
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateLocation_InvalidLocation_ThrowsArgumentNullException()
    {
        Incident incident = _mockIncidents[0];
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateLocation(incident, null!);
        });
    }

    /// <summary>
    /// Controller throws when invalid incident passed as argument to UpdateLocation
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateLocation_InvalidIncident_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => {
            await _incidentController.UpdateLocation(_invalidIncident,
                new IncidentLocation(Latitude: 0.0, Longitude: 0.0));
        });
    }

    /// <summary>
    /// Update location of incident which is valid but same location it is currently in
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateLocation_ValidIncidentSameLocation_ReturnsTrue()
    {
        Incident incident = _mockIncidents[0];

        Assert.AreEqual(new IncidentLocation(Latitude: 0.0, Longitude: 0.0), incident.Location);
        bool result = await _incidentController.UpdateLocation(incident, new IncidentLocation(Latitude: 0.0, Longitude: 0.0));
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Update location of incident which is valid with different location it is currently in
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateLocation_ValidIncidentDifferentLocation_ReturnsTrue()
    {
        Incident incident = _mockIncidents[0];
        IncidentLocation newLocation = new IncidentLocation(
                    Latitude: RandomDouble(-90, 90),
                    Longitude: RandomDouble(-180, 180)
            );

        Assert.AreNotEqual(newLocation, incident.Location);
        bool result = await _incidentController.UpdateLocation(incident, newLocation);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Controller throws when null inicdent passed as argument to UpdateSeverity
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateSeverity_NullIncident_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateSeverity(null!, IncidentSeverity.High);
        });
    }

    /// <summary>
    /// Controller throws when invalid severity passed as argument to UpdateSeverity
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateSeverity_InvalidSeverity_ThrowsArgumentOutOfRangeException()
    {
        Incident incident = _mockIncidents[0];
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => {
            await _incidentController.UpdateSeverity(incident, (IncidentSeverity)int.MaxValue!);
        });
    }

    /// <summary>
    /// Controller throws when invalid incident passed as argument to UpdateSeverity
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateSeverity_InvalidIncident_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => {
            await _incidentController.UpdateSeverity(_invalidIncident, IncidentSeverity.High);
        });
    }

    /// <summary>
    /// Update severity of incident which is valid but same severity it is currently in. 
    /// todo(@shreesh): update this test when backend changes
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateSeverity_ValidIncidentSameSeverity_ReturnsTrue()
    {
        Incident incident = _mockIncidents[0];

        Assert.AreEqual(IncidentSeverity.Medium, incident.Severity);
        bool result = await _incidentController.UpdateSeverity(incident, IncidentSeverity.Medium);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Update severity of incident which is valid with a different severity then it is currently in. 
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateSeverity_ValidIncidentDifferentSeverity_ReturnsTrue()
    {
        Incident incident = _mockIncidents[0];
        Assert.AreNotEqual(IncidentSeverity.High, incident.Severity);
        bool result = await _incidentController.UpdateSeverity(incident, IncidentSeverity.High);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Controller throws when null incident is passed as argument to UpdateStatus
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateStatus_NullIncident_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateStatus(null!, IncidentStatus.Resolved);
        });
    }

    /// <summary>
    /// Controller throws when invalid status is passed as argument to UpdateStatus
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateStatus_InvalidStatus_ThrowsArgumentOutOfRangeException()
    {
        Incident incident = _mockIncidents[0];
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => {
            await _incidentController.UpdateStatus(incident, (IncidentStatus)int.MaxValue!);
        });
    }

    /// <summary>
    /// Controller throws when invalid incident is passed as argument to UpdateStatus
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateStatus_InvalidIncident_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => {
            await _incidentController.UpdateStatus(_invalidIncident, IncidentStatus.InProgress);
        });
    }


    /// <summary>
    /// Update status of incident which is valid but same status it is currently in. 
    /// todo(@shreesh): update this test when backend changes
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateStatus_ValidIncidentSameStatus_ReturnsTrue()
    {
        Incident incident = _mockIncidents[0];

        Assert.AreEqual(IncidentStatus.Reported, incident.Status);
        bool result = await _incidentController.UpdateStatus(incident, IncidentStatus.Reported);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Update status of incident which is valid but different status then it is currently in. 
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task UpdateStatus_ValidIncidentDifferentStatus_ReturnsTrue()
    {
        Incident incident = _mockIncidents[0];
        Assert.AreNotEqual(IncidentStatus.Resolved, incident.Status);
        bool result = await _incidentController.UpdateStatus(incident, IncidentStatus.Resolved);
        Assert.IsTrue(result);
    }
}
