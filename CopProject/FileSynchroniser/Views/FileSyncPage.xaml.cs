// Author : Murali

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FileSynchroniser.Models;
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
/// Page that lists the synchronised folder and lets the user add, delete, refresh and sync files.
/// Navigate to it with a <see cref="FileSyncSession"/> as the parameter.
/// </summary>
public sealed partial class FileSyncPage : Page
{
    /// <summary>
    /// ViewModel the page binds to. It implements <see cref="IUserNotificationHandler"/>, so the
    /// controller can call OnUserJoined / OnUserLeft / SetAvailableUsers on it directly.
    /// </summary>
    public FileSyncViewModel ViewModel { get; }

    private NetworkSyncHandler? _syncHandler;
    private FileSyncSession? _session;

    public FileSyncPage()
    {
        // Created here (on the UI thread) so UiDispatcher can capture the DispatcherQueue.
        ViewModel = new FileSyncViewModel(new Synchroniser());

        InitializeComponent();
    }

    /// <summary>
    /// Reads the session from the navigation parameter and starts listening on the meeting's communicator.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is FileSyncSession session)
        {
            AttachSession(session);
        }
        else
        {
            StatusTextBlock.Text = "Not connected to a meeting.";
        }
    }

    /// <summary>
    /// Creates the sync handler on the meeting's communicator (no new sockets are opened)
    /// and loads the peers that are already in the meeting.
    /// </summary>
    private void AttachSession(FileSyncSession session)
    {
        if (_syncHandler != null)
        {
            return;
        }

        _session = session;

        _syncHandler = new NetworkSyncHandler(
            ViewModel.RootDirectory,
            session.Communicator,
            session.IsHost,
            session.LocalEndpoint,
            session.HostEndpoint);

        _syncHandler.OnLog += msg => DispatcherQueue.TryEnqueue(() => StatusTextBlock.Text = msg);

        _syncHandler.OnSyncCompleted += () => DispatcherQueue.TryEnqueue(() => {
            ViewModel.RefreshCommand.Execute(null);
            StatusTextBlock.Text = "[sync] synchronisation finished successfully!";
        });

        if (session.InitialPeers != null)
        {
            ViewModel.SetAvailableUsers(session.InitialPeers);
        }

        // live join / leave updates from the controller
        session.RegisterHandler?.Invoke(ViewModel);
    }

    /// <summary>
    /// Pulls the files of the selected (or typed) peer into this machine.
    /// </summary>
    private void OnSyncButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        NetworkSyncHandler? handler = _syncHandler;
        if (handler == null)
        {
            StatusTextBlock.Text = "Not connected to a meeting.";
            return;
        }

        if (!IPAddress.TryParse(PeerIpTextBox.Text.Trim(), out IPAddress? ip)
            || !int.TryParse(PeerPortTextBox.Text.Trim(), out int port)
            || port is < 1 or > 65535)
        {
            StatusTextBlock.Text = "Select a peer, or enter a valid peer IP and port.";
            return;
        }

        var peer = new IPEndPoint(ip, port);
        StatusTextBlock.Text = $"Syncing from {peer}...";

        // reading the manifest and files can be slow, so keep it off the UI thread
        Task.Run(() => {
            try
            {
                handler.StartSync(peer);
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(() => StatusTextBlock.Text = $"Network sync error: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Fills the peer IP and port boxes from the selected peer's details.
    /// </summary>
    private void OnPeerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedPeer is not ControllerUser selected)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(selected.Ip))
        {
            PeerIpTextBox.Text = selected.Ip;
        }

        // the host's entry in the user list can carry a stale port 0; a client knows the real one
        int port = selected.Port;
        if (port <= 0 && selected.IsHost && _session?.HostEndpoint != null)
        {
            port = _session.HostEndpoint.Port;
        }

        if (port > 0)
        {
            PeerPortTextBox.Text = port.ToString();
        }
    }

    /// <summary>
    /// Unsubscribes from the meeting's communicator and disposes the ViewModel when leaving the page.
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _session?.UnregisterHandler?.Invoke(ViewModel);
        _session = null;

        // the communicator belongs to the meeting: only unsubscribe, never dispose it
        _syncHandler?.Dispose();
        _syncHandler = null;

        ViewModel.Dispose();
        base.OnNavigatedFrom(e);
    }

    /// <summary>
    /// Opens a file picker and adds the chosen file(s) to the synchronised folder.
    /// </summary>
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
    /// Returns a name that is not already used in the batch, e.g. "report (2).pdf".
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
