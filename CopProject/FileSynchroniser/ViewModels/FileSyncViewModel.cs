// Author : Murali

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Xml.Linq;
using FileSynchroniser.Models;
using Utils;

namespace FileSynchroniser.ViewModels;

/// <summary>
/// Display model for a single file in the synchronised root directory.
/// </summary>
public class SyncFileItem
{

    public string RelativePath { get; init; } = string.Empty;
    public string SizeText { get; init; } = string.Empty;
    public string LastModifiedText { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel for the synchroniser view.
/// </summary>
public class FileSyncViewModel : INotifyPropertyChanged, IDisposable, IUserNotificationHandler
{
    private readonly ISync _sync;
    private readonly IFileOperations _files;
    private readonly IUiDispatcher _dispatcher;
    private readonly Func<string, IEnumerable<FileInfo>> _enumerateFiles;
    private readonly RelayCommand _deleteSelectedCommand;

    private SyncFileItem? _selectedFile;
    private ControllerUser? _selectedPeer;
    private string _statusMessage = "Ready";
    private bool _hasError;

    /// <summary>
    /// Creates the view model. A null <paramref name="dispatcher"/> uses the real UI dispatcher
    /// (or runs inline when no UI exists); a null <paramref name="enumerateFiles"/> uses the real file system.
    /// </summary>
    // Synchroniser is public; ISync is internal, so the constructor takes the concrete type.
    public FileSyncViewModel(Synchroniser synchroniser, IUiDispatcher? dispatcher = null,
        Func<string, IEnumerable<FileInfo>>? enumerateFiles = null)
    {
        ArgumentNullException.ThrowIfNull(synchroniser);

        _sync = synchroniser;
        _files = synchroniser;
        _dispatcher = dispatcher ?? UiDispatcher.Create();
        _enumerateFiles = enumerateFiles ?? EnumerateAllFiles;

        RefreshCommand = new RelayCommand(Refresh);
        _deleteSelectedCommand = new RelayCommand(DeleteSelected, CanDeleteSelected);

        _sync.OnSyncComplete += HandleSyncComplete;
        _sync.OnSyncError += HandleSyncError;

        RootDirectory = _files.GetDirectory();
        LoadFiles();
    }

    /// <summary>
    /// Raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Root directory being synchronised.
    /// </summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Files currently listed in the root directory.
    /// </summary>
    public ObservableCollection<SyncFileItem> Files { get; } = new();

    /// <summary>
    /// Runs a synchronisation and reloads the file list.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Deletes the currently selected file.
    /// </summary>
    public ICommand DeleteSelectedCommand => _deleteSelectedCommand;

    /// <summary>
    /// File currently selected in the list.
    /// </summary>
    public SyncFileItem? SelectedFile
    {
        get => _selectedFile;
        set {
            if (_selectedFile == value) { return; }
            _selectedFile = value;
            OnPropertyChanged(nameof(SelectedFile));
            _deleteSelectedCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// List of available meeting participants for synchronization.
    /// </summary>
    public ObservableCollection<ControllerUser> AvailablePeers { get; } = new();

    /// <summary>
    /// The currently selected peer to synchronize with.
    /// </summary>
    public ControllerUser? SelectedPeer
    {
        get => _selectedPeer;
        set {
            if (_selectedPeer == value)
            {
                return;
            }
            _selectedPeer = value;
            OnPropertyChanged(nameof(SelectedPeer));
        }
    }

    /// <summary>
    /// Sets the list of active peers from the controller.
    /// </summary>
    public void SetAvailableUsers(IEnumerable<ControllerUser> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        RunOnUi(() => {
            AvailablePeers.Clear();
            foreach (ControllerUser user in users)
            {
                AvailablePeers.Add(user);
            }

            if (SelectedPeer == null || !AvailablePeers.Contains(SelectedPeer))
            {
                SelectedPeer = AvailablePeers.FirstOrDefault();
            }
        });
    }

    /// <summary>
    /// Called when a new participant joins the meeting.
    /// </summary>
    public void OnUserJoined(ControllerUser user)
    {
        if (user == null)
        {
            return;
        }

        RunOnUi(() => {
            if (!AvailablePeers.Any(p => p.UserKey == user.UserKey || (p.Ip == user.Ip && p.Port == user.Port)))
            {
                AvailablePeers.Add(user);
                if (SelectedPeer == null)
                {
                    SelectedPeer = user;
                }
            }
        });
    }

    /// <summary>
    /// Called when a participant leaves the meeting.
    /// </summary>
    public void OnUserLeft(ControllerUser user)
    {
        if (user == null)
        {
            return;
        }

        RunOnUi(() => {
            ControllerUser? match = AvailablePeers.FirstOrDefault(p => p.UserKey == user.UserKey || (p.Ip == user.Ip && p.Port == user.Port));
            if (match != null)
            {
                AvailablePeers.Remove(match);
                if (SelectedPeer == match)
                {
                    SelectedPeer = AvailablePeers.FirstOrDefault();
                }
            }
        });
    }

    /// <summary>
    /// Message shown to the user about the latest operation.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set {
            if (_statusMessage == value) { return; }
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    /// <summary>
    /// True when the status message describes an error.
    /// </summary>
    public bool HasError
    {
        get => _hasError;
        private set {
            if (_hasError == value) { return; }
            _hasError = value;
            OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>
    /// Synchronises with the peer, then reloads the file list.
    /// </summary>
    private void Refresh()
    {
        HasError = false;
        _sync.Synchronise();   // raises OnSyncComplete / OnSyncError
        LoadFiles();
    }

    /// <summary>
    /// Deletes the selected file and reloads the list on success.
    /// </summary>
    private void DeleteSelected()
    {
        if (SelectedFile is null)
        {
            return;
        }

        string path = SelectedFile.RelativePath;
        HasError = false;

        IReadOnlyDictionary<string, bool> results = _files.DeleteFile(new[] { path });

        if (results.TryGetValue(path, out bool deleted) && deleted)
        {
            StatusMessage = $"Deleted '{path}'.";
            LoadFiles();
        }
        // On failure, Synchroniser raises OnSyncError, which updates the status.
    }

    /// <summary>
    /// Delete is allowed only when a file is selected.
    /// </summary>
    private bool CanDeleteSelected()
    {
        return SelectedFile is not null;
    }

    /// <summary>
    /// Saves one or more files picked by the user (map of relative path to content)
    /// and reloads the list if at least one was saved. To add a single file, pass
    /// a dictionary with one entry.
    /// </summary>
    public void AddFile(IReadOnlyDictionary<string, byte[]> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (files.Count == 0)
        {
            return;
        }

        HasError = false;

        IReadOnlyDictionary<string, bool> results = _files.SaveFile(files);

        int savedCount = results.Count(r => r.Value);
        bool anyFailed = savedCount < files.Count;

        if (!anyFailed)
        {
            StatusMessage = savedCount == 1
                ? $"Saved '{results.First(r => r.Value).Key}'."
                : $"Saved {savedCount} files.";
        }
        // On failure, Synchroniser raises one OnSyncError summary listing every
        // failed file, which updates the status. It is not overwritten here.

        if (savedCount > 0)
        {
            LoadFiles();
        }
    }

    /// <summary>
    /// Reads the folder listing into <see cref="Files"/>.
    /// </summary>
    private void LoadFiles()
    {
        Files.Clear();
        SelectedFile = null;

        try
        {
            IEnumerable<FileInfo> infos = _enumerateFiles(RootDirectory)
               .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase);

            foreach (FileInfo info in infos)
            {
                Files.Add(new SyncFileItem {
                    RelativePath = Path.GetRelativePath(RootDirectory, info.FullName),
                    SizeText = FormatSize(info.Length),
                    LastModifiedText = info.LastWriteTime.ToString("g")
                });
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError(ex.Message);
        }
    }

    /// <summary>
    /// Updates the status message when a sync finishes.
    /// </summary>
    private void HandleSyncComplete()
    {
        RunOnUi(() => StatusMessage = $"Synchronised at {DateTime.Now:T}.");
    }

    /// <summary>
    /// Shows the error raised by a failed sync.
    /// </summary>
    private void HandleSyncError(string message)
    {
        RunOnUi(() => ShowError(message));
    }

    /// <summary>
    /// Sets the error flag and status message.
    /// </summary>
    private void ShowError(string message)
    {
        HasError = true;
        StatusMessage = message;
    }

    /// <summary>
    /// Runs the action on the UI thread, since events may be raised from any thread.
    /// </summary>
    private void RunOnUi(Action action)
    {
        _dispatcher.Run(action);
    }

    /// <summary>
    /// Lists every file under the root directory using the real file system.
    /// </summary>
    private static IEnumerable<FileInfo> EnumerateAllFiles(string root)
    {
        return new DirectoryInfo(root).EnumerateFiles("*", SearchOption.AllDirectories);
    }

    /// <summary>
    /// Converts a byte count to a readable size such as "1.5 MB".
    /// </summary>
    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{size:0.##} {units[unit]}";
    }

    /// <summary>
    /// Notifies the view that a property changed.
    /// </summary>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Unsubscribes from sync events.
    /// </summary>
    public void Dispose()
    {
        _sync.OnSyncComplete -= HandleSyncComplete;
        _sync.OnSyncError -= HandleSyncError;
    }
}
