
using System;
using Incidents.ViewModels.Utils;

namespace Incidents.ViewModels;

/// <summary>
/// ViewModel for The items displayed in CorrelateincidentDialog. 
/// It has check box, Titile and Id of the Non correlated Incident.
/// </summary>
public sealed class CorrelateIncidentItemViewModel : ViewModelBase
{
    private bool _isSelected;

    public Guid Id { get; }
    public string Title { get; }

    /// <summary>
    ///  The check box value for the incident item. 
    ///  True if the user has selected this incident to be correlated with the current incident.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set {
            if (_isSelected == value) { return; }
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    /// <summary>
    /// Constructs the ViewModel for a single incident item in the CorrelateIncidentsDialog.
    /// </summary>
    /// <param name="incident"></param>
    public CorrelateIncidentItemViewModel(Incident incident)
    {
        Id = incident.Id;
        Title = incident.Title;
    }
}
