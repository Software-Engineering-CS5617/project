using Cloud.Crash;
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
        CrashHandler.Install();
        UnhandledException += (_, e) => CrashHandler.Current?.Report(e.Exception, true, "WinUI");
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
