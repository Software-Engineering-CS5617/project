using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Incidents.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incidents.ViewModels;

/// <summary>
/// ViewModel for the Incident Details page that shows an incident and lets the user edit it.
/// </summary>
public sealed class IncidentDetailsPageViewModel : ViewModelBase
{
    private readonly IIncidentController _incidentController;
    private readonly string _currentUserId;

    private Incident? _incident;
    private bool _isEditMode;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    // Editable properties
    private IncidentStatus _editableStatus;
    private IncidentCategory _editableCategory;
    private IncidentSeverity _editableSeverity;
    private double _editableLatitude;
    private double _editableLongitude;
    private string? _editableAddress;

    /// <summary>
    /// Creates the ViewModel and builds the dropdown options and commands.
    /// </summary>
    public IncidentDetailsPageViewModel(
        IIncidentController incidentController,
        string currentUserId)
    {
        _incidentController = incidentController ?? throw new ArgumentNullException(nameof(incidentController));
        _currentUserId = currentUserId;

        StatusOptions = Enum.GetValues(typeof(IncidentStatus)).Cast<IncidentStatus>().ToList();
        CategoryOptions = Enum.GetValues(typeof(IncidentCategory)).Cast<IncidentCategory>().ToList();
        SeverityOptions = Enum.GetValues(typeof(IncidentSeverity)).Cast<IncidentSeverity>().ToList();

        ToggleEditModeCommand = new RelayCommand(ToggleEditMode);
        CancelEditCommand = new RelayCommand(CancelEdit);
        SaveChangesCommand = new RelayCommand(async () => await SaveChangesAsync());
        UnresolveIncidentCommand = new RelayCommand(async () => await UnresolveIncidentAsync());
    }

    /// <summary>
    /// Notifies the UI about every property that depends on the incident status
    /// or on the edit mode, so buttons and panels always match the current state.
    /// </summary>
    private void RefreshStatusVisibility()
    {
        OnPropertyChanged(nameof(IsResolved));
        OnPropertyChanged(nameof(EditButtonVisibility));
        OnPropertyChanged(nameof(UnresolveButtonVisibility));
        OnPropertyChanged(nameof(EditActionsVisibility));
        OnPropertyChanged(nameof(EditVisibility));
        OnPropertyChanged(nameof(ReadOnlyVisibility));
        OnPropertyChanged(nameof(EditButtonText));
    }

    /// <summary>
    /// Reloads the incident from the controller, then refreshes the status-dependent UI.
    /// The refresh is called AFTER the new incident is assigned (and outside the
    /// Incident setter) so the UI never reads a half-updated state.
    /// </summary>
    private async Task ReloadIncidentAsync(Guid incidentId)
    {
        Incident = await _incidentController.GetIncidentById(incidentId);
        RefreshStatusVisibility();
    }

    /// <summary>
    /// Incident currently shown on the page.
    /// </summary>
    public Incident? Incident
    {
        get => _incident;
        private set {
            _incident = value;

            // Always raise, even if the controller returned the same instance
            // (its Status may have been changed in place).
            OnPropertyChanged(nameof(Incident));
            RefreshStatusVisibility();
        }
    }

    /// <summary>
    /// True while the user is editing the incident.
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        set {
            if (_isEditMode == value) { return; }
            _isEditMode = value;
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsReadOnlyMode));
            RefreshStatusVisibility();
        }
    }

    /// <summary>
    /// Opposite of IsEditMode.
    /// </summary>
    public bool IsReadOnlyMode => !IsEditMode;

    // Direct Visibility properties for XAML binding

    /// <summary>
    /// Shows the edit controls only in edit mode.
    /// </summary>
    public Visibility EditVisibility => IsEditMode ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Shows the read-only view only when not editing.
    /// </summary>
    public Visibility ReadOnlyVisibility => IsReadOnlyMode ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Text on the edit button, which changes while editing.
    /// </summary>
    public string EditButtonText => IsEditMode ? "Editing..." : "Edit Incident";

    /// <summary>
    /// True while a load or save is in progress.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set {
            if (_isBusy == value) { return; }
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    /// <summary>
    /// Opposite of IsBusy, used to enable controls while idle.
    /// </summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Load or save error shown to the user.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set {
            if (_errorMessage == value) { return; }
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(ErrorVisibility));
        }
    }

    /// <summary>
    /// Shows the error text only when there is an error message.
    /// </summary>
    public Visibility ErrorVisibility =>
        string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Bool to check if the status it resolves
    /// </summary>
    public bool IsResolved => Incident?.Status == IncidentStatus.Resolved;

    /// <summary>
    /// The visibility of EditButton
    /// </summary>
    public Visibility EditButtonVisibility => IsResolved || IsEditMode ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// The visibility of UnResolve Button
    /// </summary>
    public Visibility UnresolveButtonVisibility => IsResolved ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Visibility of EditActions (Add Evidence / Add Incidents), hidden when resolved
    /// </summary>
    public Visibility EditActionsVisibility => IsResolved ? Visibility.Collapsed : Visibility.Visible;

    // Editable bindings

    /// <summary>
    /// Status being edited.
    /// </summary>
    public IncidentStatus EditableStatus
    {
        get => _editableStatus;
        set {
            if (_editableStatus == value) { return; }
            _editableStatus = value;
            OnPropertyChanged(nameof(EditableStatus));
        }
    }

    /// <summary>
    /// Category being edited.
    /// </summary>
    public IncidentCategory EditableCategory
    {
        get => _editableCategory;
        set {
            if (_editableCategory == value) { return; }
            _editableCategory = value;
            OnPropertyChanged(nameof(EditableCategory));
        }
    }

    /// <summary>
    /// Severity being edited.
    /// </summary>
    public IncidentSeverity EditableSeverity
    {
        get => _editableSeverity;
        set {
            if (_editableSeverity == value) { return; }
            _editableSeverity = value;
            OnPropertyChanged(nameof(EditableSeverity));
        }
    }

    /// <summary>
    /// Latitude being edited.
    /// </summary>
    public double EditableLatitude
    {
        get => _editableLatitude;
        set {
            if (_editableLatitude == value) { return; }
            _editableLatitude = value;
            OnPropertyChanged(nameof(EditableLatitude));
        }
    }

    /// <summary>
    /// Longitude being edited.
    /// </summary>
    public double EditableLongitude
    {
        get => _editableLongitude;
        set {
            if (_editableLongitude == value) { return; }
            _editableLongitude = value;
            OnPropertyChanged(nameof(EditableLongitude));
        }
    }

    /// <summary>
    /// Address being edited.
    /// </summary>
    public string? EditableAddress
    {
        get => _editableAddress;
        set {
            if (_editableAddress == value) { return; }
            _editableAddress = value;
            OnPropertyChanged(nameof(EditableAddress));
        }
    }

    /// <summary>
    /// Choices for the status dropdown.
    /// </summary>
    public List<IncidentStatus> StatusOptions { get; }

    /// <summary>
    /// Choices for the category dropdown.
    /// </summary>
    public List<IncidentCategory> CategoryOptions { get; }

    /// <summary>
    /// Choices for the severity dropdown.
    /// </summary>
    public List<IncidentSeverity> SeverityOptions { get; }

    /// <summary>
    /// Evidence items attached to the incident.
    /// </summary>
    public ObservableCollection<IncidentEvidence> Evidences { get; } = [];

    /// <summary>
    /// History of changes made to the incident, newest first.
    /// </summary>
    public ObservableCollection<IChange> ChangeTimeline { get; } = [];

    /// <summary>
    /// Incidents connected to the currently displayed incident.
    /// </summary>
    public ObservableCollection<Incident> CorrelatedIncidents { get; } = [];

    /// <summary>
    /// Switches between read-only and edit mode.
    /// </summary>
    public RelayCommand ToggleEditModeCommand { get; }

    /// <summary>
    /// Discards edits and returns to read-only mode.
    /// </summary>
    public RelayCommand CancelEditCommand { get; }

    /// <summary>
    /// Saves the edited values.
    /// </summary>
    public RelayCommand SaveChangesCommand { get; }

    /// <summary>
    /// Changes a resolved incident back to InProgress.
    /// </summary>
    public RelayCommand UnresolveIncidentCommand { get; }

    /// <summary>
    /// Loads the incident with the given id and fills the editable fields and lists.
    /// </summary>
    public async Task LoadIncidentAsync(Guid incidentId)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await ReloadIncidentAsync(incidentId);

            if (Incident != null)
            {
                ResetEditableFields();
                RefreshCollections();
                await RefreshCorrelatedIncidentsAsync();
                RefreshStatusVisibility(); // make sure buttons match the loaded status
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load incident: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Flips edit mode on or off and resets the editable fields to the saved values.
    /// </summary>
    public void ToggleEditMode()
    {
        if (Incident == null || IsResolved) { return; }
        ResetEditableFields();
        IsEditMode = !IsEditMode;
    }

    /// <summary>
    /// Throws away the edits and leaves edit mode.
    /// </summary>
    public void CancelEdit()
    {
        ResetEditableFields();
        IsEditMode = false;
    }

    /// <summary>
    /// Sends only the changed values to the controller, then reloads the incident.
    /// </summary>
    public async Task SaveChangesAsync()
    {
        if (Incident == null || IsResolved) { return; }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // 1. Update Status if changed
            if (Incident.Status != EditableStatus)
            {
                await _incidentController.UpdateStatus(Incident, EditableStatus);
            }

            // 2. Update Category if changed
            if (Incident.Category != EditableCategory)
            {
                await _incidentController.UpdateCategory(Incident, EditableCategory);
            }

            // 3. Update Severity if changed
            if (Incident.Severity != EditableSeverity)
            {
                await _incidentController.UpdateSeverity(Incident, EditableSeverity);
            }

            // 4. Update Location if changed
            IncidentLocation newLocation = new(EditableLatitude, EditableLongitude, EditableAddress);
            if (Incident.Location.Latitude != EditableLatitude ||
                Incident.Location.Longitude != EditableLongitude ||
                Incident.Location.Address != EditableAddress)
            {
                await _incidentController.UpdateLocation(Incident, newLocation);
            }

            // Reload to sync latest incident object state and timeline
            await ReloadIncidentAsync(Incident.Id);
            ResetEditableFields();
            RefreshCollections();

            IsEditMode = false;
            RefreshStatusVisibility(); // status may have changed through the edit form
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save changes: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Changes a resolved incident back to InProgress.
    /// </summary>
    public async Task UnresolveIncidentAsync()
    {
        if (Incident == null || !IsResolved) { return; }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await _incidentController.UpdateStatus(
                Incident,
                IncidentStatus.InProgress);

            // Refreshes the buttons immediately after the status change
            await ReloadIncidentAsync(Incident.Id);

            ResetEditableFields();
            RefreshCollections();
            await RefreshCorrelatedIncidentsAsync();

            IsEditMode = false;
            RefreshStatusVisibility();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to unresolve incident: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Adds new evidence to the incident and refreshes the lists.
    /// </summary>
    public async Task AddEvidenceItemsAsync(List<(IncidentEvidence, byte[])> newEvidences)
    {
        if (Incident == null || IsResolved || newEvidences.Count == 0) { return; }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await _incidentController.AddEvidence(Incident, newEvidences);

            await ReloadIncidentAsync(Incident.Id);
            RefreshCollections();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add evidence: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Copies the saved incident values into the editable fields.
    /// </summary>
    private void ResetEditableFields()
    {
        if (Incident == null) { return; }

        EditableStatus = Incident.Status;
        EditableCategory = Incident.Category;
        EditableSeverity = Incident.Severity;
        EditableLatitude = Incident.Location.Latitude;
        EditableLongitude = Incident.Location.Longitude;
        EditableAddress = Incident.Location.Address;
    }

    /// <summary>
    /// Refills the evidence and change timeline lists from the current incident.
    /// </summary>
    private void RefreshCollections()
    {
        if (Incident == null) { return; }

        Evidences.Clear();
        foreach (IncidentEvidence evidence in Incident.Evidence)
        {
            Evidences.Add(evidence);
        }

        ChangeTimeline.Clear();
        // Display latest timeline events first
        foreach (IChange change in Incident.ChangeHistory.OrderByDescending(c => c.ChangedTime))
        {
            ChangeTimeline.Add(change);
        }
    }

    /// <summary>
    /// Loads all incidents connected to the current incident.
    /// </summary>
    public async Task RefreshCorrelatedIncidentsAsync()
    {
        if (Incident == null) { return; }

        CorrelatedIncidents.Clear();

        List<Incident> correlated =
            await _incidentController.GetCorrelatedIncidents(Incident.Id);

        foreach (Incident incident in correlated)
        {
            CorrelatedIncidents.Add(incident);
        }
    }

    /// <summary>
    /// Correlates the current incident with the selected incidents.
    /// </summary>
    public async Task CorrelateIncidentsAsync(List<Guid> incidentIds)
    {
        if (Incident == null || IsResolved || incidentIds.Count == 0) { return; }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            foreach (Guid id in incidentIds)
            {
                Incident otherIncident =
                    await _incidentController.GetIncidentById(id);

                await _incidentController.CorrelateIncident(
                    Incident,
                    otherIncident);
            }

            // Reload the incident and its correlated group.
            await ReloadIncidentAsync(Incident.Id);

            RefreshCollections();
            await RefreshCorrelatedIncidentsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to correlate incidents: {ex.Message}";
            await RefreshCorrelatedIncidentsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
