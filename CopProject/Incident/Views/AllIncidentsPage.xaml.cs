using System.Diagnostics.CodeAnalysis;
using Incidents.ViewModels;
using Incidents.ViewModels.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;

namespace Incidents.Views;

/// <summary>
/// Page that lists all incidents and lets the user open one or create a new one.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class AllIncidentsPage : Page
{
    private AllIncidentsPageViewModel? ViewModel => DataContext as AllIncidentsPageViewModel;
    private IncidentPageContext? _context;

    /// <summary>
    /// Creates the page and loads its XAML layout.
    /// </summary>
    public AllIncidentsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the ViewModel from the navigation context if needed and loads the incidents.
    /// </summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IncidentPageContext context)
        {
            _context = context;
        }

        if (_context == null)
        {
            return;
        }

        if (DataContext is not AllIncidentsPageViewModel)
        {
            DataContext = new AllIncidentsPageViewModel(
                _context.IncidentController,
                _context.CurrentUserId
            );
        }

        if (ViewModel != null)
        {
            await ViewModel.LoadAsync();
        }
    }

    /// <summary>
    /// Opens the Create Incident page.
    /// </summary>
    private void CreateIncident_Click(object _, RoutedEventArgs __)
    {
        if (_context != null)
        {
            this.Frame.Navigate(typeof(CreateIncidentPage), _context);
        }
    }

    /// <summary>
    /// Opens the details page for the incident the user clicked.
    /// </summary>
    private void Incident_ItemClick(object _, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Incident incident && _context != null)
        {
            this.Frame.Navigate(
                typeof(IncidentDetailsPage),
                new IncidentDetailsPageParameters(_context, incident.Id)
            );
        }
    }

    /// <summary>
    /// Synchronizes with the cloud and refreshes the incidents list.
    /// </summary>
    private async void Sync_Click(object _, RoutedEventArgs __)
    {
        if (ViewModel != null)
        {
            await ViewModel.LoadAsync();
        }
    }
}
