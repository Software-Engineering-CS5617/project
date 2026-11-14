using System.Collections.ObjectModel;
using System.Globalization;

namespace Cloud.Crash.ViewModels;

/// <summary>
/// View model of the details page: one crash group and its recent reports.
/// </summary>
public sealed class CrashDetailsViewModel : ViewModelBase
{
    private readonly ICrashReportService _service;
    private CrashReportItem? _selectedReport;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Creates the view model.
    /// </summary>
    /// <param name="service">The service that reads crash data.</param>
    /// <param name="group">The group to show.</param>
    public CrashDetailsViewModel(ICrashReportService service, CrashGroup group)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(group);

        _service = service;
        Group = group;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>
    /// Gets the group being shown.
    /// </summary>
    public CrashGroup Group { get; }

    /// <summary>
    /// Gets the recent reports of the group, newest first.
    /// </summary>
    public ObservableCollection<CrashReportItem> Reports { get; } = [];

    /// <summary>
    /// Gets the command that reloads the reports.
    /// </summary>
    public AsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the exception type of the group.
    /// </summary>
    public string Title => Group.ExceptionType;

    /// <summary>
    /// Gets the normalized top frame.
    /// </summary>
    public string TopFrame => Group.TopFrame;

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string CategoryText => Group.Category.ToString();

    /// <summary>
    /// Gets the module name.
    /// </summary>
    public string Module => Group.Module;

    /// <summary>
    /// Gets the fingerprint.
    /// </summary>
    public string Fingerprint => Group.Fingerprint;

    /// <summary>
    /// Gets the sample message.
    /// </summary>
    public string SampleMessage => Group.SampleMessage;

    /// <summary>
    /// Gets the number of crashes as text.
    /// </summary>
    public string CountText => Group.Count.ToString(CultureInfo.CurrentCulture);

    /// <summary>
    /// Gets when the first crash happened, in local time.
    /// </summary>
    public string FirstSeenText => Group.FirstSeenUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    /// <summary>
    /// Gets when the latest crash happened, in local time.
    /// </summary>
    public string LastSeenText => Group.LastSeenUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    /// <summary>
    /// Gets or sets the selected report. Null is ignored, because a UI control sends null while its list is rebuilt.
    /// </summary>
    public CrashReportItem? SelectedReport
    {
        get => _selectedReport;
        set
        {
            if (value is not null && SetProperty(ref _selectedReport, value))
            {
                OnPropertyChanged(nameof(SelectedReportText));
                OnPropertyChanged(nameof(SelectedFramesText));
                OnPropertyChanged(nameof(SelectedSourceText));
            }
        }
    }

    /// <summary>
    /// Gets the full exception text of the selected report.
    /// </summary>
    public string SelectedReportText => SelectedReport?.Report.FullText ?? string.Empty;

    /// <summary>
    /// Gets how the selected report was caught.
    /// </summary>
    public string SelectedSourceText => SelectedReport?.SourceText ?? string.Empty;

    /// <summary>
    /// Gets the frames of the selected report's root cause, one per line.
    /// </summary>
    public string SelectedFramesText
    {
        get
        {
            CrashExceptionInfo? root = SelectedReport is null ? null : CrashFingerprinter.GetRootCause(SelectedReport.Report);

            if (root is null)
            {
                return string.Empty;
            }

            return string.Join(
                Environment.NewLine,
                root.Frames.Select(frame => frame.FileName is null
                    ? $"{frame.DeclaringType}.{frame.Method}"
                    : $"{frame.DeclaringType}.{frame.Method}  ({frame.FileName}:{frame.Line})"));
        }
    }

    /// <summary>
    /// Gets a value indicating whether reports are being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Gets the last error message, or an empty string.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
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
    /// Loads the recent reports of the group and selects the newest.
    /// </summary>
    /// <returns>A task that completes when loading is done.</returns>
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            IReadOnlyList<CrashReport> reports = await _service.GetReportsAsync(Group);

            Reports.Clear();

            foreach (CrashReport report in reports)
            {
                Reports.Add(new CrashReportItem(report));
            }

            SelectedReport = Reports.Count > 0 ? Reports[0] : null;
        }
        catch (Exception ex)
        {
            ErrorMessage = "The reports could not be loaded: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
