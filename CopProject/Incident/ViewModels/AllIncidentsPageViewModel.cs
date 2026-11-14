using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Incidents.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incidents.ViewModels;

/// <summary>
/// ViewModel for the All Incidents page that loads incidents and applies the search and filters.
/// </summary>
public sealed class AllIncidentsPageViewModel : ViewModelBase
{
    private readonly IIncidentController _incidentController;
    private readonly string _currentUserId;

    private List<Incident> _allIncidents = [];
    private string _searchText = string.Empty;

    // Dropdown selections default to "All"
    private string _selectedStatus = "All";
    private string _selectedCategory = "All";
    private string _selectedSeverity = "All";

    // Date filters
    private DateTimeOffset? _fromDate;
    private DateTimeOffset? _toDate;

    // User filter
    private string _createdByInput = string.Empty;

    /// <summary>
    /// Incidents currently shown in the list after filtering.
    /// </summary>
    public ObservableCollection<Incident> Incidents { get; } = [];

    // String collections with "All" explicitly added as option 0

    /// <summary>
    /// Choices for the status dropdown.
    /// </summary>
    public List<string> StatusOptions { get; }

    /// <summary>
    /// Choices for the category dropdown.
    /// </summary>
    public List<string> CategoryOptions { get; }

    /// <summary>
    /// Choices for the severity dropdown.
    /// </summary>
    public List<string> SeverityOptions { get; }

    /// <summary>
    /// Resets every filter back to its default value.
    /// </summary>
    public RelayCommand ClearFiltersCommand { get; }

    /// <summary>
    /// Creates the ViewModel and builds the dropdown options.
    /// </summary>
    public AllIncidentsPageViewModel(
        IIncidentController incidentController,
        string currentUserId)
    {
        _incidentController = incidentController ?? throw new ArgumentNullException(nameof(incidentController));
        _currentUserId = currentUserId;

        // Build string options list starting with "All"
        StatusOptions = ["All", .. Enum.GetNames<IncidentStatus>()];
        CategoryOptions = ["All", .. Enum.GetNames<IncidentCategory>()];
        SeverityOptions = ["All", .. Enum.GetNames<IncidentSeverity>()];

        ClearFiltersCommand = new RelayCommand(ClearFilters);
    }

    /// <summary>
    /// Text typed in the search box, matched against incident titles.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set {
            if (_searchText == value) { return; }
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
            ApplyFilter();
        }
    }

    /// <summary>
    /// Status chosen in the dropdown, or "All".
    /// </summary>
    public string SelectedStatus
    {
        get => _selectedStatus;
        set {
            if (_selectedStatus == value) { return; }
            _selectedStatus = value;
            OnPropertyChanged(nameof(SelectedStatus));
            ApplyFilter();
        }
    }

    /// <summary>
    /// Category chosen in the dropdown, or "All".
    /// </summary>
    public string SelectedCategory
    {
        get => _selectedCategory;
        set {
            if (_selectedCategory == value) { return; }
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
            ApplyFilter();
        }
    }

    /// <summary>
    /// Severity chosen in the dropdown, or "All".
    /// </summary> 
    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set {
            if (_selectedSeverity == value) { return; }
            _selectedSeverity = value;
            OnPropertyChanged(nameof(SelectedSeverity));
            ApplyFilter();
        }
    }

    /// <summary>
    /// Start of the date range filter.
    /// </summary>
    public DateTimeOffset? FromDate
    {
        get => _fromDate;
        set {
            if (_fromDate == value) { return; }
            _fromDate = value;
            OnPropertyChanged(nameof(FromDate));
            ApplyFilter();
        }
    }

    /// <summary>
    /// End of the date range filter.
    /// </summary>
    public DateTimeOffset? ToDate
    {
        get => _toDate;
        set {
            if (_toDate == value) { return; }
            _toDate = value;
            OnPropertyChanged(nameof(ToDate));
            ApplyFilter();
        }
    }

    /// <summary>
    /// User Id typed in to filter incidents by creator.
    /// </summary>
    public string CreatedByInput
    {
        get => _createdByInput;
        set {
            if (_createdByInput == value) { return; }
            _createdByInput = value;
            OnPropertyChanged(nameof(CreatedByInput));
            ApplyFilter();
        }
    }

    /// <summary>
    /// Shows the "no incidents" message only when the list is empty.
    /// </summary>
    public Visibility EmptyMessageVisibility =>
        Incidents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Loads all incidents from the controller and applies the current filters.
    /// </summary>
    public async Task LoadAsync()
    {
        await _incidentController.Refresh();
        _allIncidents = await _incidentController.GetAllIncidents();
        ApplyFilter();
    }

    /// <summary>
    /// Resets all filters to their defaults and refreshes the list once.
    /// </summary>
    public void ClearFilters()
    {
        _searchText = string.Empty;
        _selectedStatus = "All";
        _selectedCategory = "All";
        _selectedSeverity = "All";
        _fromDate = null;
        _toDate = null;
        _createdByInput = string.Empty;

        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(SelectedSeverity));
        OnPropertyChanged(nameof(FromDate));
        OnPropertyChanged(nameof(ToDate));
        OnPropertyChanged(nameof(CreatedByInput));

        ApplyFilter();
    }

    /// <summary>
    /// Builds a filter from the current inputs and refills the list with the matching incidents, newest first.
    /// </summary>
    private void ApplyFilter()
    {
        string search = SearchText.Trim();
        string? titlePattern = string.IsNullOrWhiteSpace(search) ? null : Regex.Escape(search);

        // Convert string selection to nullable enum ("All" will fail to parse and return null)
        IncidentStatus? statusFilter = Enum.TryParse<IncidentStatus>(SelectedStatus, out IncidentStatus s) ? s : null;
        IncidentCategory? categoryFilter = Enum.TryParse<IncidentCategory>(SelectedCategory, out IncidentCategory c) ? c : null;
        IncidentSeverity? severityFilter = Enum.TryParse<IncidentSeverity>(SelectedSeverity, out IncidentSeverity v) ? v : null;
        string createdByFilter = CreatedByInput.Trim();

        IncidentFilter filter = new(
            Title: titlePattern,
            Status: statusFilter,
            Category: categoryFilter,
            Severity: severityFilter,
            FromDateTime: FromDate?.DateTime,
            ToDateTime: ToDate?.DateTime,
            CreatedBy: createdByFilter,
            Location: null
        );

        List<Incident> filteredIncidents = _allIncidents
            .Where(i => IncidentFilterHelper.Matches(i, filter))
            .OrderByDescending(i => i.UpdateTime)
            .ToList();

        Incidents.Clear();

        foreach (Incident incident in filteredIncidents)
        {
            Incidents.Add(incident);
        }

        OnPropertyChanged(nameof(EmptyMessageVisibility));
    }
}
