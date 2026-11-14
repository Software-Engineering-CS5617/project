using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Incidents.Test.Implementations;
using Incidents;
using Incidents.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace Incidents.Test.ViewModels;

[TestClass]
public class CreateIncidentPageViewModelTest
{
    private IIncidentController _controller = null!;
    private CreateIncidentPageViewModel _viewModel = null!;

    private readonly string _currentUserId =
            "11111111-1111-1111-1111-111111111111";

    /// <summary>
    /// Initializes the real IncidentController and CreateIncidentPageViewModel
    /// before each test.
    /// </summary>
    [TestInitialize]
    public async Task Setup()
    {
        IncidentCloudStorageService storageService = new();
        IncidentFileSynchroniser fileSynchroniser = new();

        _controller = await IncidentController.CreateAsync(storageService, fileSynchroniser);
        _viewModel = new CreateIncidentPageViewModel(_controller, _currentUserId);
    }

    // Constructor Initialization tests

    /// <summary>
    /// Constructor initializes all properties with their default values.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesDefaultValues()
    {
        Assert.AreEqual(string.Empty, _viewModel.Title);
        Assert.AreEqual(string.Empty, _viewModel.Description);

        Assert.AreEqual(0.0, _viewModel.Latitude);
        Assert.AreEqual(0.0, _viewModel.Longitude);

        Assert.IsNull(_viewModel.Address);

        Assert.AreEqual(string.Empty, _viewModel.ErrorMessage);

        Assert.IsFalse(_viewModel.IsSubmitting);
        Assert.IsTrue(_viewModel.IsNotSubmitting);

        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);

        Assert.IsEmpty(_viewModel.Evidences);
    }

    /// <summary>
    /// Constructor initializes Categories using all IncidentCategory enum values.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesCategories()
    {
        IncidentCategory[] expected = Enum.GetValues<IncidentCategory>();

        CollectionAssert.AreEqual(expected, _viewModel.Categories.ToArray());
    }

    /// <summary>
    /// Constructor initializes Severities using all IncidentSeverity enum values.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesSeverities()
    {
        IncidentSeverity[] expected = Enum.GetValues<IncidentSeverity>();

        CollectionAssert.AreEqual(expected, _viewModel.Severities.ToArray());
    }

    /// <summary>
    /// Constructor selects the first available category and severity.
    /// </summary>
    [TestMethod]
    public void Constructor_SelectsFirstCategoryAndSeverity()
    {
        Assert.AreEqual(_viewModel.Categories[0], _viewModel.SelectedCategory);
        Assert.AreEqual(_viewModel.Severities[0], _viewModel.SelectedSeverity);
    }

    /// <summary>
    /// Constructor throws ArgumentNullException when controller is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullController_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CreateIncidentPageViewModel(null!, _currentUserId));
    }

    // Property setter tests

    /// <summary>
    /// Changing Title raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Title_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.Title = "Vehicle accident",
            nameof(_viewModel.Title));
    }

    /// <summary>
    /// Setting Title to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Title_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.Title = "Vehicle accident";

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.Title = "Vehicle accident",
            nameof(_viewModel.Title));
    }

    /// <summary>
    /// Changing Description raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Description_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.Description = "Accident near the junction",
            nameof(_viewModel.Description));
    }

    /// <summary>
    /// Setting Description to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Description_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.Description = "Test description";

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.Description = "Test description",
            nameof(_viewModel.Description));
    }

    /// <summary>
    /// Changing SelectedCategory raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SelectedCategory_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.SelectedCategory = IncidentCategory.Theft,
            nameof(_viewModel.SelectedCategory));
    }

    /// <summary>
    /// Setting SelectedCategory to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SelectedCategory_SameValue_DoesNotRaisePropertyChanged()
    {
        IncidentCategory category = _viewModel.SelectedCategory;

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.SelectedCategory = category,
            nameof(_viewModel.SelectedCategory));
    }

    /// <summary>
    /// Changing SelectedSeverity raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SelectedSeverity_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.SelectedSeverity = IncidentSeverity.Critical,
            nameof(_viewModel.SelectedSeverity));
    }

    /// <summary>
    /// Setting SelectedSeverity to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SelectedSeverity_SameValue_DoesNotRaisePropertyChanged()
    {
        IncidentSeverity severity = _viewModel.SelectedSeverity;

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.SelectedSeverity = severity,
            nameof(_viewModel.SelectedSeverity));
    }

    /// <summary>
    /// Changing Latitude raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Latitude_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.Latitude = 10.5,
            nameof(_viewModel.Latitude));
    }

    /// <summary>
    /// Setting Latitude to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Latitude_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.Latitude = 10.5;

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.Latitude = 10.5,
            nameof(_viewModel.Latitude));
    }

    /// <summary>
    /// Changing Longitude raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Longitude_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.Longitude = 76.5,
            nameof(_viewModel.Longitude));
    }

    /// <summary>
    /// Setting Longitude to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Longitude_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.Longitude = 76.5;

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.Longitude = 76.5,
            nameof(_viewModel.Longitude));
    }

    /// <summary>
    /// Changing Address raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Address_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.Address = "Palakkad",
            nameof(_viewModel.Address));
    }

    /// <summary>
    /// Setting Address to null when it is already null does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void Address_NullValue_DoesNotRaisePropertyChanged()
    {
        Assert.IsNull(_viewModel.Address);

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.Address = null,
            nameof(_viewModel.Address));
    }

    // ErrorMessage and ErrorVisibility tests

    /// <summary>
    /// Setting ErrorMessage raises PropertyChanged for ErrorMessage and ErrorVisibility.
    /// </summary>
    [TestMethod]
    public void ErrorMessage_ChangedValue_RaisesPropertyChanged()
    {
        List<string?> changedProperties = [];

        _viewModel.PropertyChanged += (_, e) =>
            changedProperties.Add(e.PropertyName);

        _viewModel.ErrorMessage = "Title is required.";

        Assert.AreEqual("Title is required.", _viewModel.ErrorMessage);

        CollectionAssert.AreEqual(
            new[]
            {
                nameof(_viewModel.ErrorMessage),
                nameof(_viewModel.ErrorVisibility)
            },
            changedProperties);
    }

    /// <summary>
    /// Setting ErrorMessage to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void ErrorMessage_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.ErrorMessage = "Title is required.";

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.ErrorMessage = "Title is required.",
            nameof(_viewModel.ErrorMessage));
    }

    /// <summary>
    /// ErrorVisibility is Visible when ErrorMessage contains text.
    /// </summary>
    [TestMethod]
    public void ErrorVisibility_NonEmptyErrorMessage_ReturnsVisible()
    {
        _viewModel.ErrorMessage = "Title is required.";

        Assert.AreEqual(Visibility.Visible, _viewModel.ErrorVisibility);
    }

    /// <summary>
    /// ErrorVisibility is Collapsed when ErrorMessage is empty.
    /// </summary>
    [TestMethod]
    public void ErrorVisibility_EmptyErrorMessage_ReturnsCollapsed()
    {
        _viewModel.ErrorMessage = string.Empty;

        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);
    }

    /// <summary>
    /// ErrorVisibility is Collapsed when ErrorMessage contains only whitespace.
    /// </summary>
    [TestMethod]
    public void ErrorVisibility_WhitespaceErrorMessage_ReturnsCollapsed()
    {
        _viewModel.ErrorMessage = "   ";

        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);
    }

    // IsSubmitting tests

    /// <summary>
    /// Changing IsSubmitting raises PropertyChanged for IsSubmitting and IsNotSubmitting.
    /// </summary>
    [TestMethod]
    public void IsSubmitting_ChangedValue_RaisesPropertyChanged()
    {
        List<string?> changedProperties = [];

        _viewModel.PropertyChanged += (_, e) =>
            changedProperties.Add(e.PropertyName);

        _viewModel.IsSubmitting = true;

        Assert.IsTrue(_viewModel.IsSubmitting);
        Assert.IsFalse(_viewModel.IsNotSubmitting);

        CollectionAssert.AreEqual(
            new[]
            {
                nameof(_viewModel.IsSubmitting),
                nameof(_viewModel.IsNotSubmitting)
            },
            changedProperties);
    }

    /// <summary>
    /// Setting IsSubmitting to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void IsSubmitting_SameValue_DoesNotRaisePropertyChanged()
    {
        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.IsSubmitting = false,
            nameof(_viewModel.IsSubmitting));

        Assert.IsFalse(_viewModel.IsSubmitting);
        Assert.IsTrue(_viewModel.IsNotSubmitting);
    }

    // Submission validation tests

    /// <summary>
    /// Submitting without a title sets the required-title error message.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_EmptyTitle_SetsErrorMessage()
    {
        _viewModel.Description = "Test description";
        _viewModel.SubmitCommand.Execute(null);

        Assert.AreEqual("Title is required.", _viewModel.ErrorMessage);
        Assert.IsEmpty(await _controller.GetAllIncidents());
    }

    /// <summary>
    /// Submitting with a whitespace-only title sets the required-title error message.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_WhitespaceTitle_SetsErrorMessage()
    {
        _viewModel.Title = "   ";
        _viewModel.Description = "Test description";
        _viewModel.SubmitCommand.Execute(null);

        Assert.AreEqual("Title is required.", _viewModel.ErrorMessage);
        Assert.IsEmpty(await _controller.GetAllIncidents());
    }

    /// <summary>
    /// Submitting without a description sets the required-description error message.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_EmptyDescription_SetsErrorMessage()
    {
        _viewModel.Title = "Vehicle accident";
        _viewModel.SubmitCommand.Execute(null);

        Assert.AreEqual("Description is required.", _viewModel.ErrorMessage);
        Assert.IsEmpty(await _controller.GetAllIncidents());
    }

    /// <summary>
    /// Submitting with a whitespace-only description sets the required-description error message.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_WhitespaceDescription_SetsErrorMessage()
    {
        _viewModel.Title = "Vehicle accident";
        _viewModel.Description = "   ";
        _viewModel.SubmitCommand.Execute(null);

        Assert.AreEqual("Description is required.", _viewModel.ErrorMessage);
        Assert.IsEmpty(await _controller.GetAllIncidents());
    }

    /// <summary>
    /// Submitting with latitude below -90 sets a validation error.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_LatitudeBelowMinimum_SetsErrorMessage()
    {
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        _viewModel.Latitude = -90.1;
        _viewModel.SubmitCommand.Execute(null);

        Assert.AreEqual("Latitude must be between -90 and 90.", _viewModel.ErrorMessage);
        Assert.IsEmpty(await _controller.GetAllIncidents());
    }

    /// <summary>
    /// Submitting with latitude above 90 sets a validation error.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_LatitudeAboveMaximum_SetsErrorMessage()
    {
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        _viewModel.Latitude = 90.1;
        _viewModel.SubmitCommand.Execute(null);

        Assert.AreEqual("Latitude must be between -90 and 90.", _viewModel.ErrorMessage);
        Assert.IsEmpty(await _controller.GetAllIncidents());
    }

    /// <summary>
    /// Submitting with longitude below -180 sets a validation error.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_LongitudeBelowMinimum_SetsErrorMessage()
    {
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        _viewModel.Longitude = -180.1;
        _viewModel.SubmitCommand.Execute(null);

        Assert.AreEqual("Longitude must be between -180 and 180.", _viewModel.ErrorMessage);
        Assert.IsEmpty(await _controller.GetAllIncidents());
    }

    /// <summary>
    /// Submitting with longitude above 180 sets a validation error.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_LongitudeAboveMaximum_SetsErrorMessage()
    {
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        _viewModel.Longitude = 180.1;
        _viewModel.SubmitCommand.Execute(null);

        Assert.AreEqual("Longitude must be between -180 and 180.", _viewModel.ErrorMessage);
        Assert.IsEmpty(await _controller.GetAllIncidents());
    }

    // Successful submission tests

    /// <summary>
    /// Submitting valid details creates an incident through the real controller
    /// and raises the IncidentCreated event.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_ValidDetails_CreatesIncidentAndRaisesEvent()
    {
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        bool incidentCreated = false;
        _viewModel.IncidentCreated += (_, _) =>
            incidentCreated = true;

        await VMTestHelpers.SubmitAndWaitForIncidentCreatedAsync(_viewModel);

        List<Incident> incidents = await _controller.GetAllIncidents();

        Assert.HasCount(1, incidents);

        Incident createdIncident = incidents[0];

        Assert.AreEqual("Vehicle accident", createdIncident.Title);
        Assert.AreEqual("Test incident description", createdIncident.Description);
        Assert.AreEqual(_currentUserId, createdIncident.CreatedBy);
        Assert.AreEqual(_viewModel.SelectedCategory, createdIncident.Category);
        Assert.AreEqual(_viewModel.SelectedSeverity, createdIncident.Severity);
        Assert.AreEqual(IncidentStatus.Reported, createdIncident.Status);
        Assert.IsTrue(incidentCreated);
    }

    /// <summary>
    /// Successful submission trims whitespace from the title and description.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_ValidDetails_TrimsTitleAndDescription()
    {
        _viewModel.Title = "  Vehicle accident  ";
        _viewModel.Description = "  Test incident description  ";
        _viewModel.Latitude = 10.0;
        _viewModel.Longitude = 76.0;

        await VMTestHelpers.SubmitAndWaitForIncidentCreatedAsync(_viewModel);
        List<Incident> incidents = await _controller.GetAllIncidents();

        Assert.HasCount(1, incidents);
        Assert.AreEqual("Vehicle accident", incidents[0].Title);
        Assert.AreEqual("Test incident description", incidents[0].Description);
    }

    /// <summary>
    /// Successful submission clears an existing error message.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_ValidDetails_ClearsExistingErrorMessage()
    {
        _viewModel.ErrorMessage = "Previous validation error.";
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        await VMTestHelpers.SubmitAndWaitForIncidentCreatedAsync(_viewModel);

        Assert.AreEqual(string.Empty, _viewModel.ErrorMessage);
        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);
    }

    /// <summary>
    /// Successful submission resets IsSubmitting to false.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_ValidDetails_ResetsIsSubmitting()
    {
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        await VMTestHelpers.SubmitAndWaitForIncidentCreatedAsync(_viewModel);
        await VMTestHelpers.WaitUntilSubmissionCompletesAsync(_viewModel);

        Assert.IsFalse(_viewModel.IsSubmitting);
        Assert.IsTrue(_viewModel.IsNotSubmitting);
    }

    /// <summary>
    /// An exception during submission sets the failure error message
    /// and resets IsSubmitting.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_ExceptionOccurs_SetsErrorMessageAndResetsSubmitting()
    {
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        _viewModel.IncidentCreated += (_, _) => {
            throw new InvalidOperationException("Test exception.");
        };
        _viewModel.SubmitCommand.Execute(null);
        await VMTestHelpers.WaitUntilSubmissionCompletesAsync(_viewModel);

        Assert.AreEqual("Failed to create incident: Test exception.", _viewModel.ErrorMessage);
        Assert.IsFalse(_viewModel.IsSubmitting);
        Assert.IsTrue(_viewModel.IsNotSubmitting);
    }

    /// <summary>
    /// Successful submission without IncidentCreated subscribers
    /// still creates the incident.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_NoEventSubscribers_CreatesIncidentSuccessfully()
    {
        VMTestHelpers.SetValidIncidentDetails(_viewModel);
        _viewModel.SubmitCommand.Execute(null);
        await VMTestHelpers.WaitUntilSubmissionCompletesAsync(_viewModel);
        List<Incident> incidents = await _controller.GetAllIncidents();

        Assert.HasCount(1, incidents);
        Assert.AreEqual("Vehicle accident", incidents[0].Title);
        Assert.AreEqual(string.Empty, _viewModel.ErrorMessage);
        Assert.IsFalse(_viewModel.IsSubmitting);
    }
}
