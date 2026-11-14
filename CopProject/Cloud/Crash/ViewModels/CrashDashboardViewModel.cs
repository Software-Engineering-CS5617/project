using System.Collections.ObjectModel;

namespace Cloud.Crash.ViewModels;

/// <summary>
/// View model of the crash dashboard: a filterable list of crash groups.
/// </summary>
public sealed class CrashDashboardViewModel : ViewModelBase
{
    /// <summary>
    /// The filter option that matches everything.
    /// </summary>
    public const string AllOption = "All";

    private readonly ICrashReportService _service;
    private readonly CrashHandler? _handler;
    private List<CrashGroup> _allGroups = [];
    private string _searchText = string.Empty;
    private string _selectedCategory = AllOption;
    private string _selectedModule = AllOption;
    private List<string> _moduleOptions = [AllOption];
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private int _pendingCount;

    /// <summary>
    /// Creates the view model.
    /// </summary>
    /// <param name="service">The service that reads crash data.</param>
    /// <param name="handler">The handler used by the test-crash button; defaults to <see cref="CrashHandler.Current"/>.</param>
    public CrashDashboardViewModel(ICrashReportService service, CrashHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(service);

        _service = service;
        _handler = handler;
        ReportTestCrashCommand = new AsyncRelayCommand(ReportTestCrashAsync);
        CategoryOptions = [AllOption, .. Enum.GetNames<CrashCategory>()];
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        RetryPendingCommand = new AsyncRelayCommand(RetryPendingAsync);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
    }

    /// <summary>
    /// Gets the groups that match the current filters.
    /// </summary>
    public ObservableCollection<CrashGroupItem> Groups { get; } = [];

    /// <summary>
    /// Gets the reports that are saved on this device and not yet uploaded.
    /// </summary>
    public ObservableCollection<CrashReportItem> PendingReports { get; } = [];

    /// <summary>
    /// Gets a value indicating whether any report is saved only on this device.
    /// </summary>
    public bool HasPendingReports => PendingReports.Count > 0;

    /// <summary>
    /// Gets the command that reports a test crash without closing the app.
    /// </summary>
    public AsyncRelayCommand ReportTestCrashCommand { get; }

    /// <summary>
    /// Gets the category filter choices.
    /// </summary>
    public List<string> CategoryOptions { get; }

    /// <summary>
    /// Gets the module filter choices, built from the loaded groups.
    /// </summary>
    public List<string> ModuleOptions
    {
        get => _moduleOptions;
        private set => SetProperty(ref _moduleOptions, value);
    }

    /// <summary>
    /// Gets the command that reloads the groups.
    /// </summary>
    public AsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command that uploads reports saved on this device.
    /// </summary>
    public AsyncRelayCommand RetryPendingCommand { get; }

    /// <summary>
    /// Gets the command that resets all filters.
    /// </summary>
    public RelayCommand ClearFiltersCommand { get; }

    /// <summary>
    /// Gets or sets the text that groups are searched for in the exception type, top frame, message and fingerprint.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected category, or <see cref="AllOption"/>. Null is ignored, because a UI control sends null while its list is rebuilt.
    /// </summary>
    public string SelectedCategory
    {
        get => _selectedCategory;
        set {
            if (value is not null && SetProperty(ref _selectedCategory, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected module, or <see cref="AllOption"/>. Null is ignored, because a UI control sends null while its list is rebuilt.
    /// </summary>
    public string SelectedModule
    {
        get => _selectedModule;
        set {
            if (value is not null && SetProperty(ref _selectedModule, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether groups are being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    /// <summary>
    /// Gets the last error message, or an empty string.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is an error to show.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Gets a message about the last action, or an empty string.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is a status message to show.
    /// </summary>
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>
    /// Gets the number of reports saved on this device that have not been uploaded.
    /// </summary>
    public int PendingCount
    {
        get => _pendingCount;
        private set {
            if (SetProperty(ref _pendingCount, value))
            {
                OnPropertyChanged(nameof(HasPending));
                OnPropertyChanged(nameof(PendingMessage));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether reports are waiting for upload.
    /// </summary>
    public bool HasPending => PendingCount > 0;

    /// <summary>
    /// Gets a sentence about the reports waiting for upload.
    /// </summary>
    public string PendingMessage => $"{PendingCount} crash report(s) saved on this device have not been uploaded yet.";

    /// <summary>
    /// Gets a value indicating whether the list is empty and nothing is loading.
    /// </summary>
    public bool IsEmpty => !IsLoading && Groups.Count == 0;

    /// <summary>
    /// Loads the groups from the service.
    /// </summary>
    /// <returns>A task that completes when loading is done.</returns>
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            _allGroups = (await _service.GetGroupsAsync()).ToList();
            PendingCount = _service.GetPendingCount();
            LoadPending();
            RebuildModuleOptions();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = "The crash groups could not be loaded: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Uploads reports saved on this device, then reloads.
    /// </summary>
    /// <returns>A task that completes when the retry is done.</returns>
    public async Task RetryPendingAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            int uploaded = await _service.RetryPendingAsync();
            StatusMessage = uploaded == 0 ? "Nothing could be uploaded." : $"Uploaded {uploaded} report(s).";
        }
        catch (Exception ex)
        {
            ErrorMessage = "The pending reports could not be uploaded: " + ex.Message;
        }

        PendingCount = _service.GetPendingCount();
        LoadPending();
    }

    /// <summary>
    /// Reports a test crash through the crash handler without closing the app, then refreshes the local list.
    /// </summary>
    /// <returns>A task that completes when the report is done.</returns>
    public async Task ReportTestCrashAsync()
    {
        CrashHandler? handler = _handler ?? CrashHandler.Current;

        if (handler is null)
        {
            StatusMessage = "The crash handler is not installed.";
            return;
        }

        CrashReportOutcome outcome = await Task.Run(
            () => handler.Report(new InvalidOperationException("Deliberate test crash from the dashboard"), false, "Dashboard"));

        StatusMessage = outcome switch {
            CrashReportOutcome.Uploaded => "Test crash uploaded to the cloud. It shows under the uploaded groups once Azure has processed it.",
            CrashReportOutcome.SavedLocally => "The test crash could not be uploaded and is saved on this device.",
            CrashReportOutcome.Skipped => "The test crash was skipped because the report limit was reached.",
            _ => "The test crash could not be reported."
        };

        PendingCount = _service.GetPendingCount();
        LoadPending();
    }

    /// <summary>
    /// Resets the search text and both filters.
    /// </summary>
    public void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = AllOption;
        SelectedModule = AllOption;
    }

    private void LoadPending()
    {
        PendingReports.Clear();

        foreach (CrashReport report in _service.GetPendingReports())
        {
            PendingReports.Add(new CrashReportItem(report));
        }

        OnPropertyChanged(nameof(HasPendingReports));
    }

    private void RebuildModuleOptions()
    {
        List<string> options = [AllOption, .. _allGroups.Select(group => group.Module).Distinct().Order()];

        _selectedModule = options.Contains(_selectedModule) ? _selectedModule : AllOption;
        ModuleOptions = options;

        // The control resets its selection when the list is replaced, so announce the kept value again.
        OnPropertyChanged(nameof(SelectedModule));
    }

    private void ApplyFilters()
    {
        IEnumerable<CrashGroup> filtered = _allGroups;

        if (_selectedCategory != AllOption)
        {
            filtered = filtered.Where(group => group.Category.ToString() == _selectedCategory);
        }

        if (_selectedModule != AllOption)
        {
            filtered = filtered.Where(group => group.Module == _selectedModule);
        }

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            string text = _searchText.Trim();
            filtered = filtered.Where(group => Matches(group, text));
        }

        Groups.Clear();

        foreach (CrashGroup group in filtered)
        {
            Groups.Add(new CrashGroupItem(group));
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    private static bool Matches(CrashGroup group, string text)
    {
        return group.ExceptionType.Contains(text, StringComparison.OrdinalIgnoreCase)
            || group.TopFrame.Contains(text, StringComparison.OrdinalIgnoreCase)
            || group.SampleMessage.Contains(text, StringComparison.OrdinalIgnoreCase)
            || group.Fingerprint.Contains(text, StringComparison.OrdinalIgnoreCase);
    }
}
