using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Incidents.ViewModels.Utils;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.IO;

namespace Incidents.ViewModels;

/// <summary>
/// ViewModel for the dialog where the user adds a file and description as incident evidence.
/// </summary>
public sealed class AddEvidenceDialogViewModel : ViewModelBase
{
    private string _filePath = string.Empty;
    private string _description = string.Empty;
    private readonly IntPtr _windowHandle;

    /// <summary>
    /// Creates the ViewModel with the window handle the file picker needs.
    /// </summary>
    /// <param name="windowHandle">Handle of the window that owns the file picker.</param>
    public AddEvidenceDialogViewModel(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        BrowseFileCommand = new RelayCommand(async () => await BrowseFileAsync());
    }

    /// <summary>
    /// Opens the file picker when the Browse button is clicked.
    /// </summary>
    public RelayCommand BrowseFileCommand { get; }

    /// <summary>
    /// Path of the file chosen as evidence.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set {
            if (_filePath == value) { return; }
            _filePath = value;
            OnPropertyChanged(nameof(FilePath));
            OnPropertyChanged(nameof(IsValid));
        }
    }

    /// <summary>
    /// Text describing the evidence.
    /// </summary>
    public string Description
    {
        get => _description;
        set {
            if (_description == value) { return; }
            _description = value;
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsValid));
        }
    }

    /// <summary>
    /// True when both a file path and a description have been entered.
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(Description);

    /// <summary>
    /// Opens a Windows File Picker and assigns the selected file's path to FilePath.
    /// </summary>
    /// <remarks>
    /// Excluded from code coverage because it directly interacts with Windows UI APIs,
    /// which require actual UI interaction and are unsuitable for unit testing.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    private async Task BrowseFileAsync()
    {
        try
        {
            if (_windowHandle == IntPtr.Zero)
            {
                Debug.WriteLine("AppServices.WindowHandle is IntPtr.Zero!");
                return;
            }

            FileOpenPicker picker = new();
            InitializeWithWindow.Initialize(picker, _windowHandle);

            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            picker.FileTypeFilter.Add("*");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                FilePath = file.Path;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open file picker: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an evidence record and reads the selected file's contents.
    /// </summary>
    /// <param name="uploadedBy">Id of the user adding the evidence.</param>
    /// <returns>Evidence metadata and its file contents.</returns>
    public async Task<(IncidentEvidence Evidence, byte[] Content)> BuildEvidenceAsync(
        string uploadedBy)
    {
        if (!IsValid)
        {
            throw new InvalidOperationException(
                "A file and description are required.");
        }

        byte[] content = await File.ReadAllBytesAsync(FilePath);

        IncidentEvidence evidence = new(
            Id: Guid.NewGuid(),
            Description: Description.Trim(),
            FilePath: FilePath,
            UploadTime: DateTime.Now,
            UploadedBy: uploadedBy
        );

        return (evidence, content);
    }
}
