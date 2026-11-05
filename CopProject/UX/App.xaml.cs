using System;
using System.IO;
using Microsoft.UI.Xaml;
using UX.Models;
using UX.ViewModels;

namespace UX;

// Team modules are NOT listed here. Put a module's DLL in the Modules folder and it is found.
public partial class App : Application
{
    MainWindow? window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecureMeet");

        var auth = new AuthService(folder);
        var session = new MeetingSession(auth);
        var modules = new ModuleCatalog(Path.Combine(AppContext.BaseDirectory, "Modules"));

        var shell = new ShellViewModel(
            session,
            new LoginViewModel(auth, session),
            new DashboardViewModel(session, modules.MeetingService, folder),
            new MeetingViewModel(session),
            new ProfileViewModel(auth, session));

        window = new MainWindow();
        window.Attach(shell, modules);
        window.Activate();

        modules.Initialize(session);
    }
}
