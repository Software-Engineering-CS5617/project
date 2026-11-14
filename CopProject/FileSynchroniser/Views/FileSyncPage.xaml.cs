// Author : Murali

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using FileSynchroniser.Models;
using Controller;
using FileSynchroniser.Network;
using FileSynchroniser.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Utils;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace FileSynchroniser.Views;

[ExcludeFromCodeCoverage]
/// <summary>
/// Page that lists the synchronised folder and lets the user add, delete and refresh files.
/// </summary>
public sealed partial class FileSyncPage : Page
{
    /// <summary>
    /// ViewModel that the page binds to.
    /// </summary>
    public FileSyncViewModel ViewModel { get; }

    private SimulatedCommunicator? _communicator;
    private NetworkSyncHandler? _syncHandler;
    private IControllerService? _controller;
    private int _currentPort = 0;

    /// <summary>
    /// Creates the page and its ViewModel.
    /// </summary>
    public FileSyncPage()
    {
        // Created here (on the UI thread) so UiDispatcher can capture the DispatcherQueue.
        ViewModel = new FileSyncViewModel(new Synchroniser());

        InitializeComponent();
    }

    /// <summary>
    /// ensures communicator is initialized on the configured listening port
    /// </summary>
    private void EnsureCommunicator(int listenPort)
    {
        if (_communicator != null && _currentPort == listenPort)
        {
            return;
        }

        _communicator?.Dispose();
        _communicator = new SimulatedCommunicator(listenPort);
        _currentPort = listenPort;

        _syncHandler = new NetworkSyncHandler(ViewModel.RootDirectory, _communicator);

        _syncHandler.OnLog += msg => {
            DispatcherQueue.TryEnqueue(() => {
                StatusTextBlock.Text = msg;
            });
        };

        _syncHandler.OnSyncCompleted += () => {
            DispatcherQueue.TryEnqueue(() => {
                ViewModel.RefreshCommand.Execute(null);
                StatusTextBlock.Text = "[sync] synchronisation finished successfully!";
            });
        };
    }

    /// <summary>
    /// handles click event on the sync button
    /// </summary>
    private void OnSyncButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string peerIp = PeerIpTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(peerIp))
        {
            peerIp = "127.0.0.1";
        }

        if (!int.TryParse(MyPortTextBox.Text.Trim(), out int listenPort))
        {
            listenPort = 8001;
        }

        if (!int.TryParse(PeerPortTextBox.Text.Trim(), out int peerPort))
        {
            peerPort = 8002;
        }

        StatusTextBlock.Text = $"Connecting on port {listenPort} to {peerIp}:{peerPort}...";

        System.Threading.Tasks.Task.Run(() => {
            try
            {
                EnsureCommunicator(listenPort);

                var peerEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(peerIp), peerPort);
                _communicator!.RegisterPeer(peerEndpoint);
                _syncHandler!.DefaultPeerEndpoint = peerEndpoint;

                _syncHandler.StartSync();
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(() => {
                    StatusTextBlock.Text = $"Network sync error: {ex.Message}";
                });
            }
        });
    }

    /// <summary>
    /// preset A button click: listen 8001, target 8002
    /// </summary>
    private void OnPresetAClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MyPortTextBox.Text = "8001";
        PeerIpTextBox.Text = "127.0.0.1";
        PeerPortTextBox.Text = "8002";
    }

    /// <summary>
    /// preset B button click: listen 8002, target 8001
    /// </summary>
    private void OnPresetBClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MyPortTextBox.Text = "8002";
        PeerIpTextBox.Text = "127.0.0.1";
        PeerPortTextBox.Text = "8001";
    }

    /// <summary>
    /// Attaches controller service when navigated to during a meeting
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Navigation parameter carries the meeting controller, if any
        if (e.Parameter is IControllerService controller)
        {
            AttachController(controller);
        }
    }

    /// <summary>
    /// Stores the controller, pre-fills the local port, loads current peers, and subscribes to join/leave events.
    /// </summary>
    private void AttachController(IControllerService controller)
    {
        _controller = controller;

        // Auto-fill our own listening port if controller has one
        if (_controller.ListenPort > 0)
        {
            MyPortTextBox.Text = _controller.ListenPort.ToString();
        }

        // Populate active participants
        List<ControllerUser> users = _controller.GetActiveUsers();
        if (users != null && users.Count > 0)
        {
            // Exclude ourselves from the peer list
            List<ControllerUser> peers = _controller.CurrentUser != null
                ? users.Where(u => u.UserKey != _controller.CurrentUser.UserKey).ToList()
                : users;

            ViewModel.SetAvailableUsers(peers);
        }

        _controller.UserJoined += OnControllerUserJoined;
        _controller.UserLeft += OnControllerUserLeft;
    }

    /// <summary>
    /// Adds a newly joined meeting participant to the peer list (ignores ourselves).
    /// </summary>
    private void OnControllerUserJoined(object? sender, UserJoinedEventArgs e)
    {
        // Event may fire on a background thread; marshal to the UI thread
        DispatcherQueue.TryEnqueue(() => {
            if (e.User != null)
            {
                // Skip our own join event
                if (_controller?.CurrentUser != null && e.User.UserKey == _controller.CurrentUser.UserKey)
                {
                    return;
                }

                ViewModel.OnUserJoined(e.User);
            }
        });
    }

    /// <summary>
    /// Removes a participant who left the meeting from the peer list.
    /// </summary>
    private void OnControllerUserLeft(object? sender, UserLeftEventArgs e)
    {
        // Marshal to the UI thread before touching the ViewModel
        DispatcherQueue.TryEnqueue(() => {
            if (e.User != null)
            {
                ViewModel.OnUserLeft(e.User);
            }
        });
    }

    /// <summary>
    /// Fills the peer IP and port boxes from the selected peer's details.
    /// </summary>
    private void OnPeerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedPeer is ControllerUser selected)
        {
            // Only overwrite fields when the peer has valid values
            if (!string.IsNullOrWhiteSpace(selected.Ip))
            {
                PeerIpTextBox.Text = selected.Ip;
            }

            if (selected.Port > 0)
            {
                PeerPortTextBox.Text = selected.Port.ToString();
            }
        }
    }

    /// <summary>
    /// Reloads the active peer list from the controller and reports the result in the status text.
    /// </summary>
    private void OnRefreshPeersClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_controller != null)
        {
            List<ControllerUser> users = _controller.GetActiveUsers();

            // Exclude ourselves from the peer list
            List<ControllerUser> peers = _controller.CurrentUser != null
                ? users.Where(u => u.UserKey != _controller.CurrentUser.UserKey).ToList()
                : users;

            ViewModel.SetAvailableUsers(peers);
            StatusTextBlock.Text = $"Refreshed {peers.Count} available peer(s) from meeting.";
        }
        else
        {
            // No meeting context; user can still connect by typing IP/port
            StatusTextBlock.Text = "No active meeting controller connected. You can enter IP and Port manually.";
        }
    }

    /// <summary>
    /// Disposes the ViewModel when the user navigates away from the page.
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        if (_controller != null)
        {
            _controller.UserJoined -= OnControllerUserJoined;
            _controller.UserLeft -= OnControllerUserLeft;
            _controller = null;
        }

        _communicator?.Dispose();
        ViewModel.Dispose();
        base.OnNavigatedFrom(e);
    }

    /// <summary>
    /// Opens a file picker and adds the chosen file(s) to the synchronised folder.
    /// </summary>
    /// <param name="sender">The Add File button.</param>
    /// <param name="e">Event data for the click.</param>
    private async void AddFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");

            // WinUI 3 pickers must be tied to the window handle.
            IntPtr hwnd = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
            InitializeWithWindow.Initialize(picker, hwnd);

            IReadOnlyList<StorageFile> pickedFiles = await picker.PickMultipleFilesAsync();
            if (pickedFiles is null || pickedFiles.Count == 0)
            {
                return; // user cancelled
            }

            var filesToSave = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            var unreadable = new List<string>();

            foreach (StorageFile file in pickedFiles)
            {
                try
                {
                    byte[] content = await File.ReadAllBytesAsync(file.Path);
                    filesToSave[GetUniqueName(file.Name, filesToSave)] = content;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    unreadable.Add($"{file.Name}: {ex.Message}");
                }
            }

            // The ViewModel reports success/failure through its status message.
            if (filesToSave.Count > 0)
            {
                ViewModel.AddFile(filesToSave);
            }

            if (unreadable.Count > 0)
            {
                await ShowMessageAsync("Could not read some selected files", string.Join(Environment.NewLine, unreadable));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowMessageAsync("Could not read the selected file", ex.Message);
        }
    }

    /// <summary>
    /// Returns a name that is not already used in the batch, e.g. "report (2).pdf"
    /// when two picked files from different folders are both called "report.pdf".
    /// </summary>
    private static string GetUniqueName(string fileName, IReadOnlyDictionary<string, byte[]> existing)
    {
        if (!existing.ContainsKey(fileName))
        {
            return fileName;
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        int counter = 2;
        string candidate;

        do
        {
            candidate = $"{baseName} ({counter}){extension}";
            counter++;
        }
        while (existing.ContainsKey(candidate));

        return candidate;
    }

    /// <summary>
    /// Shows a simple message dialog with an OK button.
    /// </summary>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="message">Message text shown in the dialog.</param>
    /// <returns>A task that completes when the dialog is closed.</returns>
    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}
