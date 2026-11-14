using System.Diagnostics.CodeAnalysis;
using Cloud.Crash;
using Cloud.Crash.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;

namespace Cloud.UI.Views;

/// <summary>
/// Lists crash groups with filters. Navigate to it with a <see cref="CrashPageContext"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class CrashDashboardPage : Page
{
    private ICrashReportService? _service;

    /// <summary>
    /// Creates the page.
    /// </summary>
    public CrashDashboardPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model, available after navigation.
    /// </summary>
    public CrashDashboardViewModel? ViewModel { get; private set; }

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not CrashPageContext context)
        {
            return;
        }

        _service = context.Service;
        CrashDashboardViewModel viewModel = new(_service);
        ViewModel = viewModel;
        Bindings.Update();

        await viewModel.LoadAsync();
    }

    private void CrashApp_Click(object _, RoutedEventArgs __)
    {
        // A real crash: WinUI passes this to the application's UnhandledException handler, which reports it.
        throw new InvalidOperationException("Deliberate crash from the dashboard");
    }

    private void Group_ItemClick(object _, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CrashGroupItem item && _service is not null)
        {
            Frame.Navigate(typeof(CrashDetailsPage), new CrashDetailsPageParameters(_service, item.Group));
        }
    }
}
