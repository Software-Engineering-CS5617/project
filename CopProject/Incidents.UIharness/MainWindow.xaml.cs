using System;
using Incidents;
using Incidents.Views;
using Cloud.Persistence;
using FileSynchroniser.Models;
using Microsoft.UI.Xaml;

namespace Incidents.UIharness;

/// <summary>
/// A TEST Harness for the indenpendent development of Incidents WinUI Pages.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeAndStartAsync();
    }

    /// <summary>
    /// Methods for Initializing all the setup for the function start up.
    /// </summary>
    private async void InitializeAndStartAsync()
    {
        // Instantiate services/controllers
        IStorage storageService = await StorageFactory.CreateAsync(ModuleType.IncidentManagement);
        IFileOperations fileSynchroniser = new Synchroniser();

        IIncidentController controller = await IncidentController.CreateAsync(storageService, fileSynchroniser);
        string currentUserId = "TestUser";
        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

        IncidentPageContext context = new(controller, currentUserId, windowHandle);
        RootFrame.Navigate(typeof(AllIncidentsPage), context);
        //IncidentCreatePageWithLocation locationContext = new(context, 37.7749, -122.4194, "San Francisco, CA");
        //RootFrame.Navigate(typeof(CreateIncidentPage), locationContext);

    }
}
