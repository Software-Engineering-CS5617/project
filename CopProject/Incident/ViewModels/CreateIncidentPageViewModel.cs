using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Incidents;
using Incidents.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incidents.ViewModels;

/// <summary>
/// ViewModel for the Create Incident page that validates the form and submits a new incident.
/// </summary>
public sealed class CreateIncidentPageViewModel : ViewModelBase
{
    private readonly IIncidentController _incidentController;
    private readonly string _currentUserId;

    /// <summary>
    /// Raised after an incident is created successfully.
    /// </summary>
    public event EventHandler? IncidentCreated;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private IncidentCategory _selectedCategory;
    private IncidentSeverity _selectedSeverity;
    private double _latitude;
    private double _longitude;
    private string? _address;
    private string _errorMessage = string.Empty;
    private bool _isSubmitting;

    /// <summary>
    /// Creates the ViewModel and fills the category and severity lists with their first values selected.
    /// </summary>
    public CreateIncidentPageViewModel(
        IIncidentController incidentController,
        string currentUserId)
    {
        _incidentController = incidentController ?? throw new ArgumentNullException(nameof(incidentController));
        _currentUserId = currentUserId;

        Categories = Enum.GetValues(typeof(IncidentCategory)).Cast<IncidentCategory>().ToList();
        Severities = Enum.GetValues(typeof(IncidentSeverity)).Cast<IncidentSeverity>().ToList();

        if (Categories.Count > 0)
        {
            _selectedCategory = Categories[0];
        }

        if (Severities.Count > 0)
        {
            _selectedSeverity = Severities[0];
        }

        SubmitCommand = new RelayCommand(async () => await SubmitAsync());
    }

    /// <summary>
    /// Categories the user can choose from.
    /// </summary>
    public IReadOnlyList<IncidentCategory> Categories { get; }

    /// <summary>
    /// Severity the user can choose from.
    /// </summary>
    public IReadOnlyList<IncidentSeverity> Severities { get; }

    // todo(@sreenath): Change signature to Tuple<IncidentEvidence, byte[]> to include the bytes of the file uploaded

    /// <summary>
    /// Evidence metadata displayed in the UI.
    /// </summary>
    public ObservableCollection<IncidentEvidence> Evidences { get; } = [];

    /// <summary>
    /// Evidence metadata and corresponding file contents submitted with the incident.
    /// </summary>
    public List<(IncidentEvidence Evidence, byte[] Content)> _evidenceContents = [];

    /// <summary>
    /// Validates the form and creates the incident when Submit is clicked.
    /// </summary>
    public RelayCommand SubmitCommand { get; }

    /// <summary>
    /// Short title of the incident.
    /// </summary>
    public string Title
    {
        get => _title;
        set {
            if (_title == value) { return; }
            _title = value;
            OnPropertyChanged(nameof(Title));
        }
    }

    /// <summary>
    /// Detailed description of the incident.
    /// </summary>
    public string Description
    {
        get => _description;
        set {
            if (_description == value) { return; }
            _description = value;
            OnPropertyChanged(nameof(Description));
        }
    }

    /// <summary>
    /// Category chosen for the incident.
    /// </summary>
    public IncidentCategory SelectedCategory
    {
        get => _selectedCategory;
        set {
            if (_selectedCategory == value) { return; }
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    /// <summary>
    /// Severity chosen for the incident.
    /// </summary>
    public IncidentSeverity SelectedSeverity
    {
        get => _selectedSeverity;
        set {
            if (_selectedSeverity == value) { return; }
            _selectedSeverity = value;
            OnPropertyChanged(nameof(SelectedSeverity));
        }
    }

    /// <summary>
    /// Latitude of the incident location, from -90 to 90.
    /// </summary>
    public double Latitude
    {
        get => _latitude;
        set {
            if (_latitude == value) { return; }
            _latitude = value;
            OnPropertyChanged(nameof(Latitude));
        }
    }

    /// <summary>
    /// Longitude of the incident location, from -180 to 180.
    /// </summary>
    public double Longitude
    {
        get => _longitude;
        set {
            if (_longitude == value) { return; }
            _longitude = value;
            OnPropertyChanged(nameof(Longitude));
        }
    }

    /// <summary>
    /// Optional street address of the incident location.
    /// </summary>
    public string? Address
    {
        get => _address;
        set {
            if (_address == value) { return; }
            _address = value;
            OnPropertyChanged(nameof(Address));
        }
    }

    /// <summary>
    /// Validation or submit error shown to the user.
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
    /// True while the incident is being sent to the controller.
    /// </summary>
    public bool IsSubmitting
    {
        get => _isSubmitting;
        set {
            if (_isSubmitting == value) { return; }
            _isSubmitting = value;
            OnPropertyChanged(nameof(IsSubmitting));
            OnPropertyChanged(nameof(IsNotSubmitting));
        }
    }

    /// <summary>
    /// Opposite of IsSubmitting, used to enable the form while idle.
    /// </summary>
    public bool IsNotSubmitting => !IsSubmitting;

    /// <summary>
    /// Validates the inputs, creates the incident through the controller and raises IncidentCreated.
    /// </summary>
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Description is required.";
            return;
        }

        if (Latitude < -90 || Latitude > 90)
        {
            ErrorMessage = "Latitude must be between -90 and 90.";
            return;
        }

        if (Longitude < -180 || Longitude > 180)
        {
            ErrorMessage = "Longitude must be between -180 and 180.";
            return;
        }

        try
        {
            IsSubmitting = true;
            ErrorMessage = string.Empty;

            IncidentLocation location = new(Latitude, Longitude, Address);

            CreateIncidentRequest request = new(
                Title.Trim(),
                Description.Trim(),
                _currentUserId,
                SelectedCategory,
                SelectedSeverity,
                location,
                _evidenceContents.ToList()
            );

            await _incidentController.CreateIncident(request);

            IncidentCreated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create incident: {ex.Message}";
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
