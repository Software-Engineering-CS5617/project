using System.Diagnostics.CodeAnalysis;
using Cloud.Crash;
using Microsoft.UI.Xaml;

namespace Cloud.UIharness;

/// <summary>
/// Test application that hosts the crash dashboard on its own.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Creates the application.
    /// </summary>
    public App()
    {
        InitializeComponent();
        CrashHandler.Install();
        UnhandledException += (_, e) => CrashHandler.Current?.Report(e.Exception, true, "WinUI");
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
