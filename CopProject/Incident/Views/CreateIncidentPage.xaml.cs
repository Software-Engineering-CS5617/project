using System;
using System.Diagnostics.CodeAnalysis;
using Incidents.ViewModels;
using Incidents.ViewModels.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Incidents.Views;

/// <summary>
/// Page where the user fills in the details of a new incident and submits it.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class CreateIncidentPage : Page
{
    public CreateIncidentPageViewModel? ViewModel => DataContext as CreateIncidentPageViewModel;
    private IncidentPageContext? _context;

    /// <summary>
    /// Creates the page and loads its XAML layout.
    /// </summary>
    public CreateIncidentPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the ViewModel from the navigation context and fills in the location if one was passed.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        IncidentCreatePageWithLocation? locationContext =
            e.Parameter as IncidentCreatePageWithLocation;

        _context = locationContext?.Context
            ?? e.Parameter as IncidentPageContext;

        if (_context == null) { return; }

        if (DataContext is not CreateIncidentPageViewModel)
        {
            var vm = new CreateIncidentPageViewModel(
                _context.IncidentController,
                _context.CurrentUserId
            );

            vm.IncidentCreated += (s, args) => {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            };

            DataContext = vm;
        }

        if (locationContext != null && ViewModel != null)
        {
            ViewModel.Latitude = locationContext.Latitude;
            ViewModel.Longitude = locationContext.Longitude;
            ViewModel.Address = locationContext.Address;
        }
    }

    /// <summary>
    /// Goes back to the previous page without creating an incident.
    /// </summary>
    private void CancelButton_Click(object _, RoutedEventArgs __)
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
            XamlRoot = this.XamlRoot, // Use Page's XamlRoot
            DataContext = dialogViewModel
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialogViewModel.IsValid && ViewModel != null)
        {
            //todo(@sreenath): Add evidence as well as bytes of the file uploaded
            (IncidentEvidence Evidence, byte[] Content) evidence = await dialogViewModel.BuildEvidenceAsync(_context.CurrentUserId);
            ViewModel.Evidences.Add(evidence.Evidence);
            ViewModel._evidenceContents.Add(evidence);
        }
    }
}
