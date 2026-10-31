using Microsoft.UI.Xaml;

namespace FileSync.UIharness;

/// <summary>
/// handles application startup and launch
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// creates a new instance of the App
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// runs when the application is launched
    /// </summary>
    /// <param name="args">launch event arguments</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
