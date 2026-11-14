
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Incidents.ViewModels;

/// <summary>
/// ViewModel for the CorrelatIncidentDiaglog, where an Incident can be correlated with other Incidents.
/// </summary>
public sealed class CorrelateIncidentsDialogViewModel
{
    private readonly IIncidentController _incidentController;
    private readonly Guid _currentIncidentId;

    /// <summary>
    /// The List of Incidents ,that are not correlated with the current Incident.
    /// </summary>
    public ObservableCollection<CorrelateIncidentItemViewModel> AvailableIncidents { get; } = [];

    /// <summary>
    /// Creates the ViewModel for the CorrelateIncidentsDialog.
    /// </summary>
    public CorrelateIncidentsDialogViewModel(
        IIncidentController incidentController,
        Guid currentIncidentId)
    {
        _incidentController = incidentController
            ?? throw new ArgumentNullException(nameof(incidentController));

        _currentIncidentId = currentIncidentId;
    }

    /// <summary>
    /// Loads the list of incidents that can be correlated with the current incident,
    /// excluding already correlated incidents.
    /// </summary>
    public async Task LoadIncidentsAsync()
    {
        AvailableIncidents.Clear();

        // Get the current incident's existing correlated group.
        List<Incident> correlatedIncidents =
            await _incidentController.GetCorrelatedIncidents(_currentIncidentId);

        List<Guid> excludedIds = correlatedIncidents.Select(i => i.Id).ToList();

        excludedIds.Add(_currentIncidentId);

        // Get all incidents and exclude already-correlated ones.
        List<Incident> allIncidents = await _incidentController.GetAllIncidents();

        foreach (Incident incident in allIncidents)
        {
            if (!excludedIds.Contains(incident.Id))
            {
                AvailableIncidents.Add(new CorrelateIncidentItemViewModel(incident));
            }
        }
    }

    /// <summary>
    /// Method to get the list of selected incident Ids from the AvailableIncidents collection.
    /// </summary>
    /// <returns>List of Guids</returns>
    public List<Guid> GetSelectedIncidentIds()
    {
        return AvailableIncidents
            .Where(i => i.IsSelected)
            .Select(i => i.Id)
            .ToList();
    }
}
