using System;
using Incidents;
using Incidents.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Incidents.Test.ViewModels;

/// <summary>
/// Tests the CorrelateIncidentItemViewModel functionality.
/// </summary>
[TestClass]
public sealed class CorrelateIncidentItemViewModelTest
{
    private Incident _incident = null!;
    private CorrelateIncidentItemViewModel _viewModel = null!;

    /// <summary>
    /// Initializes an Incident and its corresponding ViewModel.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _incident = new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident",
            description: "Test incident description",
            createdBy: Guid.NewGuid().ToString(),
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.High,
            status: IncidentStatus.Reported,
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            location: new IncidentLocation(10.0, 76.0, "Test location"),
            evidence: [],
            changes: [],
            edges: []
        );

        _viewModel = new CorrelateIncidentItemViewModel(_incident);
    }

    /// <summary>
    /// Constructor initializes the incident ID and title correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesIncidentDetails()
    {
        Assert.AreEqual(_incident.Id, _viewModel.Id);
        Assert.AreEqual(_incident.Title, _viewModel.Title);
    }

    /// <summary>
    /// Constructor initializes IsSelected to false.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesIsSelectedToFalse()
    {
        Assert.IsFalse(_viewModel.IsSelected);
    }

    /// <summary>
    /// Setting IsSelected to true updates its value.
    /// </summary>
    [TestMethod]
    public void IsSelected_SetTrue_UpdatesProperty()
    {
        _viewModel.IsSelected = true;

        Assert.IsTrue(_viewModel.IsSelected);
    }

    /// <summary>
    /// Setting IsSelected to false updates its value.
    /// </summary>
    [TestMethod]
    public void IsSelected_SetFalse_UpdatesProperty()
    {
        _viewModel.IsSelected = true;

        _viewModel.IsSelected = false;

        Assert.IsFalse(_viewModel.IsSelected);
    }

    /// <summary>
    /// Changing IsSelected raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void IsSelected_SetValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.IsSelected = true,
            nameof(_viewModel.IsSelected));
    }

    /// <summary>
    /// Assigning the same IsSelected value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void IsSelected_SetSameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.IsSelected = true;

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.IsSelected = true,
            nameof(_viewModel.IsSelected));
    }
}
