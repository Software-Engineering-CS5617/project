using Microsoft.UI.Xaml;
using Utils.Diagnostics;
namespace UX;

public partial class App : Application
{
    private Window? _window;
    public App()
    {
        FileTraceListener.Initialize();
        InitializeComponent();
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
