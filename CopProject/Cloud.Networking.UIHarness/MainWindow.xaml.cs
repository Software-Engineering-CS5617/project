using Cloud.Networking;
using Cloud.UI.ViewModels;
using Cloud.UI.Views;
using Microsoft.UI.Xaml;

namespace Cloud.Networking.UIHarness;

public sealed partial class MainWindow : Window
{
    private readonly NotificationServiceViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Notification Service - UI Harness";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 760));

        // The server URL is typed in the page at runtime and remembered after the first successful connection.
        _viewModel = new NotificationServiceViewModel(MessengerFactory.GetMessenger);
        ViewHost.Content = new NotificationServiceView(_viewModel);

        Closed += async (_, _) => await _viewModel.DisposeAsync();
    }
}
