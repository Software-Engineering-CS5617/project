using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Policy;
using Incidents.ViewModels;
using Incidents.ViewModels.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Incidents.Views;

/// <summary>
/// Page that shows the details of one incident and lets the user edit it or add evidence.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class IncidentDetailsPage : Page
{
    private IncidentDetailsPageViewModel? ViewModel => DataContext as IncidentDetailsPageViewModel;
    private IncidentPageContext? _context;

    /// <summary>
    /// Creates the page and loads its XAML layout.
    /// </summary>
    public IncidentDetailsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the ViewModel from the navigation parameters and loads the selected incident.
    /// </summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IncidentDetailsPageParameters parameters)
        {
            _context = parameters.Context;

            if (DataContext is not IncidentDetailsPageViewModel)
            {
                DataContext = new IncidentDetailsPageViewModel(
                    _context.IncidentController,
                    _context.CurrentUserId
                );
            }

            if (ViewModel != null)
            {
                await ViewModel.LoadIncidentAsync(parameters.IncidentId);
            }
        }
    }

    /// <summary>
    /// Goes back to the previous page.
    /// </summary>
    private void BackButton_Click(object _, RoutedEventArgs __)
    {
        if (this.Frame.CanGoBack)
        {
            this.Frame.GoBack();
        }
    }

    /// <summary>
    /// Shows the Add Evidence dialog and adds the evidence to the incident if the user confirms.
    /// </summary>
    private async void AddEvidence_Click(object _, RoutedEventArgs __)
    {
        if (_context == null)
        {
            return;
        }

        AddEvidenceDialogViewModel dialogViewModel = new(_context.WindowHandle);

        AddEvidenceDialog dialog = new() {
            XamlRoot = this.XamlRoot,
            DataContext = dialogViewModel
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialogViewModel.IsValid && ViewModel != null)
        {
            (IncidentEvidence Evidence, byte[] Content) res = await dialogViewModel.BuildEvidenceAsync(_context.CurrentUserId);
            IncidentEvidence evidence = res.Evidence;
            byte[] content = res.Content;
            await ViewModel.AddEvidenceItemsAsync([(evidence, content)]);
        }
    }

    /// <summary>
    /// Shows the Correlate Incidents dialog and correlates the selected incidents.
    /// </summary>
    private async void CorrelateIncidents_Click(object _, RoutedEventArgs __)
    {
        if (_context == null || ViewModel?.Incident == null)
        {
            return;
        }

        CorrelateIncidentsDialogViewModel dialogViewModel = new(
            _context.IncidentController,
            ViewModel.Incident.Id
        );

        await dialogViewModel.LoadIncidentsAsync();

        CorrelateIncidentsDialog dialog = new() {
            XamlRoot = this.XamlRoot,
            DataContext = dialogViewModel
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            List<Guid> selectedIds = dialogViewModel.GetSelectedIncidentIds();

            await ViewModel.CorrelateIncidentsAsync(selectedIds);
        }
    }
}
