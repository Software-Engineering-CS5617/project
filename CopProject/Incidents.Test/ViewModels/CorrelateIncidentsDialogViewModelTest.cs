using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileSynchroniser.Models;
using Incidents;
using Incidents.Test.Implementations;
using Incidents.ViewModels;
using Cloud.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Incidents.Test.ViewModels;

/// <summary>
/// Tests the CorrelateIncidentsDialogViewModel functionality.
/// </summary>
[TestClass]
public sealed class CorrelateIncidentsDialogViewModelTest
{
    private IncidentController _incidentController = null!;

    private List<Incident> _existingIncidents = [];
    private Incident _currentIncident = null!;
    private Incident _firstIncident = null!;
    private Incident _secondIncident = null!;
    private Incident _thirdIncident = null!;

    private CorrelateIncidentsDialogViewModel _viewModel = null!;

    /// <summary>
    /// Initializes incidents, cloud storage, and IncidentController.
    /// </summary>
    [TestInitialize]
    public async Task Setup()
    {
        _currentIncident = CreateTestIncident("Current Incident");
        _firstIncident = CreateTestIncident("First Incident");
        _secondIncident = CreateTestIncident("Second Incident");
        _thirdIncident = CreateTestIncident("Third Incident");

        _existingIncidents =
        [
            _currentIncident,
            _firstIncident,
            _secondIncident,
            _thirdIncident
        ];

        await SetupIncidentController();

        _viewModel = new CorrelateIncidentsDialogViewModel(
            _incidentController,
            _currentIncident.Id);
    }

    /// <summary>
    /// Creates an Incident with the supplied title.
    /// </summary>
    private static Incident CreateTestIncident(string title)
    {
        return new Incident(
            title: title,
            description: "Test incident description.",
            createdBy: string.Empty,
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(
                Latitude: 10.0,
                Longitude: 76.0,
                Address: "Test location")
        );
    }

    /// <summary>
    /// Initializes IncidentController with prepopulated cloud storage.
    /// </summary>
    private async Task SetupIncidentController()
    {
        IncidentCloudStorageService cloudStorageService =
            new IncidentCloudStorageService();

        await SetupIncidentCloudStorageService(cloudStorageService);

        IFileOperations fileOperationsService =
            new IncidentFileSynchroniser();

        _incidentController = await IncidentController.CreateAsync(
            cloudStorageService,
            fileOperationsService);
    }

    /// <summary>
    /// Initializes cloud storage with the test incidents.
    /// </summary>
    private async Task SetupIncidentCloudStorageService(
        IncidentCloudStorageService cloudStorageService)
    {
        Dictionary<string, byte[]> payload = new();

        _existingIncidents.ForEach(incident => {
            payload.Add(
                incident.Id.ToString(),
                IncidentSerializer.Serialize(incident));
        });

        await cloudStorageService.CreateAll(payload);
    }

    /// <summary>
    /// Constructor initializes an empty AvailableIncidents collection.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesAvailableIncidents()
    {
        Assert.IsEmpty(_viewModel.AvailableIncidents);
    }

    /// <summary>
    /// Constructor throws ArgumentNullException when the controller is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullController_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CorrelateIncidentsDialogViewModel(
                null!,
                Guid.NewGuid()));
    }

    /// <summary>
    /// Loading incidents excludes the current incident.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentsAsync_ExcludesCurrentIncident()
    {
        await _viewModel.LoadIncidentsAsync();

        Assert.IsFalse(_viewModel.AvailableIncidents.Any(
            incident => incident.Id == _currentIncident.Id));
    }

    /// <summary>
    /// Loading incidents excludes incidents already correlated
    /// with the current incident.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentsAsync_ExcludesAlreadyCorrelatedIncidents()
    {
        // Retrieve the incidents loaded by the real controller.
        Incident currentIncident =
            await _incidentController.GetIncidentById(_currentIncident.Id);

        Incident firstIncident =
            await _incidentController.GetIncidentById(_firstIncident.Id);

        // Establish an existing correlation using the real controller.
        bool result = await _incidentController.CorrelateIncident(
            currentIncident,
            firstIncident);

        Assert.IsTrue(result);

        // Load the available incidents.
        await _viewModel.LoadIncidentsAsync();

        // The already-correlated incident should not be available.
        Assert.IsFalse(_viewModel.AvailableIncidents.Any(
            incident => incident.Id == firstIncident.Id));
    }

    /// <summary>
    /// Loading incidents includes incidents that are not already correlated.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentsAsync_IncludesUncorrelatedIncidents()
    {
        await _viewModel.LoadIncidentsAsync();

        Assert.IsTrue(_viewModel.AvailableIncidents.Any(
            incident => incident.Id == _firstIncident.Id));

        Assert.IsTrue(_viewModel.AvailableIncidents.Any(
            incident => incident.Id == _secondIncident.Id));

        Assert.IsTrue(_viewModel.AvailableIncidents.Any(
            incident => incident.Id == _thirdIncident.Id));
    }

    /// <summary>
    /// Loading incidents creates item ViewModels with the correct details.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentsAsync_CreatesItemsWithCorrectDetails()
    {
        await _viewModel.LoadIncidentsAsync();

        CorrelateIncidentItemViewModel? item =
            _viewModel.AvailableIncidents.FirstOrDefault(
                incident => incident.Id == _firstIncident.Id);

        Assert.IsNotNull(item);
        Assert.AreEqual(_firstIncident.Id, item.Id);
        Assert.AreEqual(_firstIncident.Title, item.Title);
        Assert.IsFalse(item.IsSelected);
    }

    /// <summary>
    /// Loading incidents again clears and reloads the collection
    /// without creating duplicate entries.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentsAsync_CalledAgain_ReloadsCollection()
    {
        await _viewModel.LoadIncidentsAsync();

        int initialCount = _viewModel.AvailableIncidents.Count;

        await _viewModel.LoadIncidentsAsync();

        Assert.HasCount(initialCount, _viewModel.AvailableIncidents);

        Assert.AreEqual(
            _viewModel.AvailableIncidents.Count,
            _viewModel.AvailableIncidents.Select(i => i.Id).Distinct().Count());
    }

    /// <summary>
    /// GetSelectedIncidentIds returns an empty list when no incidents are selected.
    /// </summary>
    [TestMethod]
    public async Task GetSelectedIncidentIds_NoSelection_ReturnsEmptyList()
    {
        await _viewModel.LoadIncidentsAsync();

        Assert.IsEmpty(_viewModel.GetSelectedIncidentIds());
    }

    /// <summary>
    /// GetSelectedIncidentIds returns only the IDs of selected incidents.
    /// </summary>
    [TestMethod]
    public async Task GetSelectedIncidentIds_SelectedIncidents_ReturnsTheirIds()
    {
        await _viewModel.LoadIncidentsAsync();

        // Select two incidents and leave the third unselected.
        _viewModel.AvailableIncidents
            .First(i => i.Id == _firstIncident.Id)
            .IsSelected = true;

        _viewModel.AvailableIncidents
            .First(i => i.Id == _secondIncident.Id)
            .IsSelected = true;

        List<Guid> selectedIds = _viewModel.GetSelectedIncidentIds();

        Assert.HasCount(2, selectedIds);

        CollectionAssert.Contains(selectedIds, _firstIncident.Id);
        CollectionAssert.Contains(selectedIds, _secondIncident.Id);
        CollectionAssert.DoesNotContain(selectedIds, _thirdIncident.Id);
    }
}
