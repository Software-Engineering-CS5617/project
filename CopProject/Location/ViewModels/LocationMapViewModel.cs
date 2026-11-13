using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Incidents;
using LocationTracker.Interfaces;
using LocationTracker.Models;
using LocationTracker.ViewModels.Utils;

namespace LocationTracker.ViewModels;

public sealed class LocationMapViewModel : ViewModelBase
{
    private readonly IMapService _mapService;
    private string _titleText = string.Empty;
    private string _selectedCategory = "All";
    private string _selectedSeverity = "All";
    private string _selectedStatus = "All";
    private DateTimeOffset? _fromDate;
    private DateTimeOffset? _toDate;
    private string _errorMessage = string.Empty;

    public ObservableCollection<MapIncident> Incidents { get; } = [];

    public event EventHandler? IncidentsChanged;

    public LocationMapViewModel(IMapService mapService)
    {
        _mapService = mapService;

        CategoryOptions = ["All", .. Enum.GetNames<IncidentCategory>()];
        SeverityOptions = ["All", .. Enum.GetNames<IncidentSeverity>()];
        StatusOptions = ["All", .. Enum.GetNames<IncidentStatus>()];
    }

    public List<string> CategoryOptions { get; }
    public List<string> SeverityOptions { get; }
    public List<string> StatusOptions { get; }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set {
            if (_errorMessage == value) { return; }
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
        }
    }

    public string TitleText
    {
        get => _titleText;
        set {
            if (_titleText == value) { return; }
            _titleText = value;
            OnPropertyChanged(nameof(TitleText));
            _ = LoadAsync();
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set {
            if (_selectedCategory == value) { return; }
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
            _ = LoadAsync();
        }
    }

    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set {
            if (_selectedSeverity == value) { return; }
            _selectedSeverity = value;
            OnPropertyChanged(nameof(SelectedSeverity));
            _ = LoadAsync();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set {
            if (_selectedStatus == value) { return; }
            _selectedStatus = value;
            OnPropertyChanged(nameof(SelectedStatus));
            _ = LoadAsync();
        }
    }

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

    public async Task LoadAsync()
    {
        if (!IsDateRangeValid())
        {
            return;
        }

        ErrorMessage = string.Empty;
        List<Incident> incidents =
            await _mapService.GetIncidentsForMapAsync(BuildFilter());

        Incidents.Clear();
        foreach (Incident incident in incidents)
        {
            Incidents.Add(new MapIncident(
                incident.Id,
                incident.Title,
                incident.Description,
                incident.Category.ToString(),
                incident.Severity.ToString(),
                incident.Status.ToString(),
                incident.Location.Latitude,
                incident.Location.Longitude,
                incident.Location.Address));
        }

        IncidentsChanged?.Invoke(this, EventArgs.Empty);
    }

    private IncidentFilter BuildFilter()
    {
        string search = TitleText.Trim();
        string? title = string.IsNullOrEmpty(search) ? null : Regex.Escape(search);

        IncidentCategory? category =
            Enum.TryParse(SelectedCategory, out IncidentCategory categoryValue)
                ? categoryValue
                : null;
        IncidentSeverity? severity =
            Enum.TryParse(SelectedSeverity, out IncidentSeverity severityValue)
                ? severityValue
                : null;
        IncidentStatus? status =
            Enum.TryParse(SelectedStatus, out IncidentStatus statusValue)
                ? statusValue
                : null;

        return new IncidentFilter(
            title,
            status,
            category,
            severity,
            FromDate?.DateTime,
            ToDate?.DateTime,
            null,
            null);
    }

    private void ApplyFilter()
    {
        if (!IsDateRangeValid())
        {
            ErrorMessage = "The start date must be before the end date.";
            return;
        }

        _ = LoadAsync();
    }

    private bool IsDateRangeValid()
    {
        return !FromDate.HasValue ||
               !ToDate.HasValue ||
               FromDate <= ToDate;
    }
}
