using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Incidents;
using Incidents.Test.Implementations;
using Incidents.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Incidents.Test.ViewModels;

[TestClass]
public class IncidentDetailsPageViewModelDisplayTest
{
    private IIncidentController _controller = null!;
    private IncidentDetailsPageViewModel _viewModel = null!;

    private readonly string _currentUserId =
        "11111111-1111-1111-1111-111111111111";

    /// <summary>
    /// Initializes the real IncidentController and IncidentDetailsPageViewModel
    /// before each test.
    /// </summary>
    [TestInitialize]
    public async Task Setup()
    {
        IncidentCloudStorageService storageService = new();
        IncidentFileSynchroniser fileSynchroniser = new();

        _controller = await IncidentController.CreateAsync(storageService, fileSynchroniser);
        _viewModel = new IncidentDetailsPageViewModel(_controller, _currentUserId);
    }

    // Constructor Initialization tests

    /// <summary>
    /// Constructor initializes the incident and state properties
    /// with their default values.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesDefaultValues()
    {
        Assert.IsNull(_viewModel.Incident);
        Assert.IsFalse(_viewModel.IsEditMode);
        Assert.IsTrue(_viewModel.IsReadOnlyMode);

        Assert.AreEqual(Visibility.Collapsed, _viewModel.EditVisibility);
        Assert.AreEqual(Visibility.Visible, _viewModel.ReadOnlyVisibility);
        Assert.AreEqual("Edit Incident", _viewModel.EditButtonText);

        Assert.IsFalse(_viewModel.IsBusy);
        Assert.IsTrue(_viewModel.IsNotBusy);

        Assert.AreEqual(string.Empty, _viewModel.ErrorMessage);
        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);

        Assert.IsEmpty(_viewModel.Evidences);
        Assert.IsEmpty(_viewModel.ChangeTimeline);
        Assert.IsEmpty(_viewModel.CorrelatedIncidents);
    }

    /// <summary>
    /// Constructor initializes StatusOptions using all IncidentStatus enum values.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesStatusOptions()
    {
        IncidentStatus[] expected = Enum.GetValues<IncidentStatus>();
        CollectionAssert.AreEqual(expected, _viewModel.StatusOptions.ToArray());
    }

    /// <summary>
    /// Constructor initializes CategoryOptions using all IncidentCategory enum values.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesCategoryOptions()
    {
        IncidentCategory[] expected = Enum.GetValues<IncidentCategory>();
        CollectionAssert.AreEqual(expected, _viewModel.CategoryOptions.ToArray());
    }

    /// <summary>
    /// Constructor initializes SeverityOptions using all IncidentSeverity enum values.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesSeverityOptions()
    {
        IncidentSeverity[] expected = Enum.GetValues<IncidentSeverity>();
        CollectionAssert.AreEqual(expected, _viewModel.SeverityOptions.ToArray());
    }

    /// <summary>
    /// Constructor throws ArgumentNullException when controller is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullController_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new IncidentDetailsPageViewModel(null!, _currentUserId));
    }

    // IsEditMode and derived property tests

    /// <summary>
    /// Changing IsEditMode raises PropertyChanged for IsEditMode
    /// and all dependent properties.
    /// </summary>
    [TestMethod]
    public void IsEditMode_ChangedValue_RaisesPropertyChanged()
    {
        List<string?> changedProperties = [];

        _viewModel.PropertyChanged += (_, e) =>
            changedProperties.Add(e.PropertyName);

        _viewModel.IsEditMode = true;

        Assert.IsTrue(_viewModel.IsEditMode);
        Assert.IsFalse(_viewModel.IsReadOnlyMode);

        CollectionAssert.AreEqual(
            new[]
            {
                nameof(_viewModel.IsEditMode),
                nameof(_viewModel.IsReadOnlyMode),
                nameof(_viewModel.IsResolved),
                nameof(_viewModel.EditButtonVisibility),
                nameof(_viewModel.UnresolveButtonVisibility),
                nameof(_viewModel.EditActionsVisibility),
                nameof(_viewModel.EditVisibility),
                nameof(_viewModel.ReadOnlyVisibility),
                nameof(_viewModel.EditButtonText)
            },
            changedProperties);
    }

    /// <summary>
    /// Setting IsEditMode to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void IsEditMode_SameValue_DoesNotRaisePropertyChanged()
    {
        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.IsEditMode = false,
            nameof(_viewModel.IsEditMode));
    }

    /// <summary>
    /// EditVisibility is Visible when IsEditMode is true.
    /// </summary>
    [TestMethod]
    public void EditVisibility_EditModeEnabled_ReturnsVisible()
    {
        _viewModel.IsEditMode = true;

        Assert.AreEqual(Visibility.Visible, _viewModel.EditVisibility);
    }

    /// <summary>
    /// EditVisibility is Collapsed when IsEditMode is false.
    /// </summary>
    [TestMethod]
    public void EditVisibility_EditModeDisabled_ReturnsCollapsed()
    {
        _viewModel.IsEditMode = false;

        Assert.AreEqual(Visibility.Collapsed, _viewModel.EditVisibility);
    }

    /// <summary>
    /// ReadOnlyVisibility is Visible when IsEditMode is false.
    /// </summary>
    [TestMethod]
    public void ReadOnlyVisibility_EditModeDisabled_ReturnsVisible()
    {
        _viewModel.IsEditMode = false;

        Assert.AreEqual(Visibility.Visible, _viewModel.ReadOnlyVisibility);
    }

    /// <summary>
    /// ReadOnlyVisibility is Collapsed when IsEditMode is true.
    /// </summary>
    [TestMethod]
    public void ReadOnlyVisibility_EditModeEnabled_ReturnsCollapsed()
    {
        _viewModel.IsEditMode = true;

        Assert.AreEqual(Visibility.Collapsed, _viewModel.ReadOnlyVisibility);
    }

    /// <summary>
    /// EditButtonText returns the appropriate text based on edit mode.
    /// </summary>
    [TestMethod]
    public void EditButtonText_ReturnsTextBasedOnEditMode()
    {
        Assert.AreEqual("Edit Incident", _viewModel.EditButtonText);

        _viewModel.IsEditMode = true;

        Assert.AreEqual("Editing...", _viewModel.EditButtonText);
    }

    // IsBusy tests

    /// <summary>
    /// Changing IsBusy raises PropertyChanged for IsBusy and IsNotBusy.
    /// </summary>
    [TestMethod]
    public void IsBusy_ChangedValue_RaisesPropertyChanged()
    {
        List<string?> changedProperties = [];

        _viewModel.PropertyChanged += (_, e) =>
            changedProperties.Add(e.PropertyName);

        _viewModel.IsBusy = true;

        Assert.IsTrue(_viewModel.IsBusy);
        Assert.IsFalse(_viewModel.IsNotBusy);

        CollectionAssert.AreEqual(
            new[]
            {
                nameof(_viewModel.IsBusy),
                nameof(_viewModel.IsNotBusy)
            },
            changedProperties);
    }

    /// <summary>
    /// Setting IsBusy to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void IsBusy_SameValue_DoesNotRaisePropertyChanged()
    {
        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.IsBusy = false,
            nameof(_viewModel.IsBusy));

        Assert.IsFalse(_viewModel.IsBusy);
        Assert.IsTrue(_viewModel.IsNotBusy);
    }

    // ErrorMessage and ErrorVisibility tests

    /// <summary>
    /// Changing ErrorMessage raises PropertyChanged for ErrorMessage
    /// and ErrorVisibility.
    /// </summary>
    [TestMethod]
    public void ErrorMessage_ChangedValue_RaisesPropertyChanged()
    {
        List<string?> changedProperties = [];

        _viewModel.PropertyChanged += (_, e) =>
            changedProperties.Add(e.PropertyName);

        _viewModel.ErrorMessage = "Test error.";

        Assert.AreEqual("Test error.", _viewModel.ErrorMessage);

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
        _viewModel.ErrorMessage = "Test error.";

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.ErrorMessage = "Test error.",
            nameof(_viewModel.ErrorMessage));
    }

    /// <summary>
    /// ErrorVisibility is Visible when ErrorMessage contains text.
    /// </summary>
    [TestMethod]
    public void ErrorVisibility_NonEmptyErrorMessage_ReturnsVisible()
    {
        _viewModel.ErrorMessage = "Test error.";

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

    // Editable property setter tests

    /// <summary>
    /// Changing EditableStatus raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableStatus_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.EditableStatus = IncidentStatus.InProgress,
            nameof(_viewModel.EditableStatus));
    }

    /// <summary>
    /// Setting EditableStatus to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableStatus_SameValue_DoesNotRaisePropertyChanged()
    {
        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.EditableStatus = default,
            nameof(_viewModel.EditableStatus));
    }

    /// <summary>
    /// Changing EditableCategory raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableCategory_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.EditableCategory = IncidentCategory.Theft,
            nameof(_viewModel.EditableCategory));
    }

    /// <summary>
    /// Setting EditableCategory to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableCategory_SameValue_DoesNotRaisePropertyChanged()
    {
        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.EditableCategory = default,
            nameof(_viewModel.EditableCategory));
    }

    /// <summary>
    /// Changing EditableSeverity raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableSeverity_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.EditableSeverity = IncidentSeverity.Critical,
            nameof(_viewModel.EditableSeverity));
    }

    /// <summary>
    /// Setting EditableSeverity to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableSeverity_SameValue_DoesNotRaisePropertyChanged()
    {
        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.EditableSeverity = default,
            nameof(_viewModel.EditableSeverity));
    }

    /// <summary>
    /// Changing EditableLatitude raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableLatitude_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.EditableLatitude = 10.5,
            nameof(_viewModel.EditableLatitude));
    }

    /// <summary>
    /// Setting EditableLatitude to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableLatitude_SameValue_DoesNotRaisePropertyChanged()
    {
        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.EditableLatitude = 0.0,
            nameof(_viewModel.EditableLatitude));
    }

    /// <summary>
    /// Changing EditableLongitude raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableLongitude_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(_viewModel,
            () => _viewModel.EditableLongitude = 76.5,
            nameof(_viewModel.EditableLongitude));
    }

    /// <summary>
    /// Setting EditableLongitude to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableLongitude_SameValue_DoesNotRaisePropertyChanged()
    {
        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.EditableLongitude = 0.0,
            nameof(_viewModel.EditableLongitude));
    }

    /// <summary>
    /// Changing EditableAddress raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableAddress_ChangedValue_RaisesPropertyChanged()
    {
        VMTestHelpers.AssertPropertyChanged(
            _viewModel,
            () => _viewModel.EditableAddress = "Palakkad",
            nameof(_viewModel.EditableAddress));
    }

    /// <summary>
    /// Setting EditableAddress to null when it is already null
    /// does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void EditableAddress_NullValue_DoesNotRaisePropertyChanged()
    {
        Assert.IsNull(_viewModel.EditableAddress);

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.EditableAddress = null,
            nameof(_viewModel.EditableAddress));
    }

    // LoadIncidentAsync tests

    /// <summary>
    /// Loading an existing incident assigns it to the ViewModel.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentAsync_ExistingIncident_LoadsIncident()
    {
        Incident createdIncident = await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        await _viewModel.LoadIncidentAsync(createdIncident.Id);

        Assert.IsNotNull(_viewModel.Incident);

        Assert.AreEqual(createdIncident.Id, _viewModel.Incident.Id);
        Assert.AreEqual(createdIncident.Title, _viewModel.Incident.Title);
        Assert.AreEqual(createdIncident.Description, _viewModel.Incident.Description);
    }

    /// <summary>
    /// Loading an incident raises PropertyChanged for Incident.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentAsync_ExistingIncident_RaisesPropertyChanged()
    {
        Incident createdIncident = await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        bool notified = false;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.Incident))
            {
                notified = true;
            }
        };

        await _viewModel.LoadIncidentAsync(createdIncident.Id);

        Assert.IsTrue(notified);
    }

    /// <summary>
    /// Loading an incident resets the editable properties
    /// to the incident's current values.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentAsync_ExistingIncident_ResetsEditableFields()
    {
        Incident createdIncident = await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        _viewModel.EditableStatus = IncidentStatus.Resolved;
        _viewModel.EditableCategory = IncidentCategory.Theft;
        _viewModel.EditableSeverity = IncidentSeverity.Critical;
        _viewModel.EditableLatitude = 50.0;
        _viewModel.EditableLongitude = 100.0;
        _viewModel.EditableAddress = "Temporary address";

        await _viewModel.LoadIncidentAsync(createdIncident.Id);

        Assert.AreEqual(createdIncident.Status, _viewModel.EditableStatus);
        Assert.AreEqual(createdIncident.Category, _viewModel.EditableCategory);
        Assert.AreEqual(createdIncident.Severity, _viewModel.EditableSeverity);
        Assert.AreEqual(createdIncident.Location.Latitude, _viewModel.EditableLatitude);
        Assert.AreEqual(createdIncident.Location.Longitude, _viewModel.EditableLongitude);
        Assert.AreEqual(createdIncident.Location.Address, _viewModel.EditableAddress);
    }

    /// <summary>
    /// Loading an incident resets IsBusy after the operation completes.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentAsync_ExistingIncident_ResetsIsBusy()
    {
        Incident createdIncident = await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        await _viewModel.LoadIncidentAsync(createdIncident.Id);

        Assert.IsFalse(_viewModel.IsBusy);
        Assert.IsTrue(_viewModel.IsNotBusy);
    }

    /// <summary>
    /// Loading an incident clears an existing error message.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentAsync_ExistingIncident_ClearsErrorMessage()
    {
        Incident createdIncident = await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        _viewModel.ErrorMessage = "Previous error.";

        await _viewModel.LoadIncidentAsync(createdIncident.Id);

        Assert.AreEqual(string.Empty, _viewModel.ErrorMessage);
        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);
    }

    /// <summary>
    /// Loading an incident refreshes the evidence and change timeline collections.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentAsync_ExistingIncident_RefreshesCollections()
    {
        Incident createdIncident = await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        await _viewModel.LoadIncidentAsync(createdIncident.Id);

        Assert.HasCount(createdIncident.Evidence.Count, _viewModel.Evidences);
        Assert.HasCount(createdIncident.ChangeHistory.Count, _viewModel.ChangeTimeline);
    }

    // ToggleEditMode tests

    /// <summary>
    /// ToggleEditMode does nothing when no incident is loaded.
    /// </summary>
    [TestMethod]
    public void ToggleEditMode_NoIncident_DoesNothing()
    {
        _viewModel.ToggleEditMode();

        Assert.IsFalse(_viewModel.IsEditMode);
        Assert.IsNull(_viewModel.Incident);
    }

    /// <summary>
    /// ToggleEditMode enables edit mode when an incident is loaded.
    /// </summary>
    [TestMethod]
    public async Task ToggleEditMode_IncidentLoaded_EnablesEditMode()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

        _viewModel.ToggleEditMode();

        Assert.IsTrue(_viewModel.IsEditMode);
        Assert.IsFalse(_viewModel.IsReadOnlyMode);
    }

    /// <summary>
    /// Calling ToggleEditMode again disables edit mode.
    /// </summary>
    [TestMethod]
    public async Task ToggleEditMode_AlreadyEditing_DisablesEditMode()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

        _viewModel.ToggleEditMode();
        _viewModel.ToggleEditMode();

        Assert.IsFalse(_viewModel.IsEditMode);
        Assert.IsTrue(_viewModel.IsReadOnlyMode);
    }

    /// <summary>
    /// ToggleEditMode resets editable fields before enabling edit mode.
    /// </summary>
    [TestMethod]
    public async Task ToggleEditMode_EditableFieldsModified_ResetsEditableFields()
    {
        Incident incident = await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

        _viewModel.EditableStatus = IncidentStatus.Resolved;
        _viewModel.EditableCategory = IncidentCategory.Theft;
        _viewModel.EditableSeverity = IncidentSeverity.Critical;
        _viewModel.EditableLatitude = 50.0;
        _viewModel.EditableLongitude = 100.0;
        _viewModel.EditableAddress = "Temporary address";

        _viewModel.ToggleEditMode();

        Assert.AreEqual(incident.Status, _viewModel.EditableStatus);
        Assert.AreEqual(incident.Category, _viewModel.EditableCategory);
        Assert.AreEqual(incident.Severity, _viewModel.EditableSeverity);
        Assert.AreEqual(incident.Location.Latitude, _viewModel.EditableLatitude);
        Assert.AreEqual(incident.Location.Longitude, _viewModel.EditableLongitude);
        Assert.AreEqual(incident.Location.Address, _viewModel.EditableAddress);
    }

    // CancelEdit tests

    /// <summary>
    /// CancelEdit disables edit mode when no incident is loaded.
    /// </summary>
    [TestMethod]
    public void CancelEdit_NoIncident_DisablesEditMode()
    {
        _viewModel.IsEditMode = true;

        _viewModel.CancelEdit();

        Assert.IsFalse(_viewModel.IsEditMode);
    }

    /// <summary>
    /// CancelEdit disables edit mode for a loaded incident.
    /// </summary>
    [TestMethod]
    public async Task CancelEdit_IncidentLoaded_DisablesEditMode()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

        _viewModel.IsEditMode = true;

        _viewModel.CancelEdit();

        Assert.IsFalse(_viewModel.IsEditMode);
        Assert.IsTrue(_viewModel.IsReadOnlyMode);
    }

    /// <summary>
    /// CancelEdit discards changes made to editable fields.
    /// </summary>
    [TestMethod]
    public async Task CancelEdit_EditableFieldsModified_ResetsEditableFields()
    {
        Incident incident = await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

        _viewModel.IsEditMode = true;

        _viewModel.EditableStatus = IncidentStatus.Resolved;
        _viewModel.EditableCategory = IncidentCategory.Theft;
        _viewModel.EditableSeverity = IncidentSeverity.Critical;
        _viewModel.EditableLatitude = 50.0;
        _viewModel.EditableLongitude = 100.0;
        _viewModel.EditableAddress = "Temporary address";

        _viewModel.CancelEdit();

        Assert.AreEqual(incident.Status, _viewModel.EditableStatus);
        Assert.AreEqual(incident.Category, _viewModel.EditableCategory);
        Assert.AreEqual(incident.Severity, _viewModel.EditableSeverity);
        Assert.AreEqual(incident.Location.Latitude, _viewModel.EditableLatitude);
        Assert.AreEqual(incident.Location.Longitude, _viewModel.EditableLongitude);
        Assert.AreEqual(incident.Location.Address, _viewModel.EditableAddress);
    }

    // CorrelatedIncidents tests

    /// <summary>
    /// Loading an incident also loads its correlated incidents.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentAsync_ExistingIncident_LoadsCorrelatedIncidents()
    {
        Incident currentIncident =
            await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        Incident correlatedIncident =
            await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        // Establish a correlation using the real controller.
        Incident storedCurrent =
            await _controller.GetIncidentById(currentIncident.Id);

        Incident storedCorrelated =
            await _controller.GetIncidentById(correlatedIncident.Id);

        await _controller.CorrelateIncident(storedCurrent, storedCorrelated);

        await _viewModel.LoadIncidentAsync(currentIncident.Id);

        Assert.IsTrue(_viewModel.CorrelatedIncidents.Any(
            incident => incident.Id == correlatedIncident.Id));
    }

    /// <summary>
    /// Loading an incident does not include unrelated incidents
    /// in the correlated incidents collection.
    /// </summary>
    [TestMethod]
    public async Task LoadIncidentAsync_ExistingIncident_ExcludesUncorrelatedIncidents()
    {
        Incident currentIncident =
            await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        Incident unrelatedIncident =
            await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        await _viewModel.LoadIncidentAsync(currentIncident.Id);

        Assert.IsFalse(_viewModel.CorrelatedIncidents.Any(
            incident => incident.Id == unrelatedIncident.Id));
    }

    /// <summary>
    /// RefreshCorrelatedIncidentsAsync does nothing when no incident is loaded.
    /// </summary>
    [TestMethod]
    public async Task RefreshCorrelatedIncidentsAsync_NoIncident_DoesNothing()
    {
        await _viewModel.RefreshCorrelatedIncidentsAsync();

        Assert.IsNull(_viewModel.Incident);
        Assert.IsEmpty(_viewModel.CorrelatedIncidents);
    }

    /// <summary>
    /// RefreshCorrelatedIncidentsAsync reloads the correlated incidents
    /// for the currently loaded incident.
    /// </summary>
    [TestMethod]
    public async Task RefreshCorrelatedIncidentsAsync_IncidentLoaded_RefreshesCollection()
    {
        Incident currentIncident =
            await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        Incident correlatedIncident =
            await VMTestHelpers.CreateIncidentAsync(_controller, _currentUserId);

        await _viewModel.LoadIncidentAsync(currentIncident.Id);

        // Create the correlation after the initial load.
        Incident storedCurrent =
            await _controller.GetIncidentById(currentIncident.Id);

        Incident storedCorrelated =
            await _controller.GetIncidentById(correlatedIncident.Id);

        await _controller.CorrelateIncident(storedCurrent, storedCorrelated);

        await _viewModel.RefreshCorrelatedIncidentsAsync();

        Assert.IsTrue(_viewModel.CorrelatedIncidents.Any(
            incident => incident.Id == correlatedIncident.Id));
    }

}
