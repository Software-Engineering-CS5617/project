using System.Diagnostics.CodeAnalysis;
using Cloud.Crash.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Cloud.UI.Views;

/// <summary>
/// Shows one crash group and its recent reports. Navigate to it with <see cref="CrashDetailsPageParameters"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class CrashDetailsPage : Page
{
    /// <summary>
    /// Creates the page.
    /// </summary>
    public CrashDetailsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model, available after navigation.
    /// </summary>
    public CrashDetailsViewModel? ViewModel { get; private set; }

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not CrashDetailsPageParameters parameters)
        {
            return;
        }

        CrashDetailsViewModel viewModel = new(parameters.Service, parameters.Group);
        ViewModel = viewModel;
        Bindings.Update();

        await viewModel.LoadAsync();
    }

    private void Back_Click(object _, RoutedEventArgs __)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}
