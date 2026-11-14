using System.Globalization;

namespace Cloud.Crash.ViewModels;

/// <summary>
/// A crash report prepared for display in a list.
/// </summary>
public sealed class CrashReportItem
{
    /// <summary>
    /// Creates the item.
    /// </summary>
    /// <param name="report">The report to display.</param>
    public CrashReportItem(CrashReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        Report = report;
    }

    private CrashSignature? _signature;

    /// <summary>
    /// Gets the underlying report.
    /// </summary>
    public CrashReport Report { get; }

    /// <summary>
    /// Gets the text shown on a report that exists only on this device.
    /// </summary>
    public string StatusText => "Saved on this device";

    /// <summary>
    /// Gets the root-cause exception type without its namespace.
    /// </summary>
    public string ExceptionTypeText
    {
        get {
            string type = Signature.ExceptionType;
            int lastDot = type.LastIndexOf('.');
            return lastDot < 0 ? type : type[(lastDot + 1)..];
        }
    }

    /// <summary>
    /// Gets the normalized top frame.
    /// </summary>
    public string TopFrame => Signature.TopFrame;

    private CrashSignature Signature => _signature ??= CrashFingerprinter.Analyze(Report);

    /// <summary>
    /// Gets when the crash happened, in local time.
    /// </summary>
    public string TimeText => Report.TimestampUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    /// <summary>
    /// Gets a one-line summary for the list.
    /// </summary>
    public string Summary => $"{TimeText}  ·  v{Report.AppVersion}  ·  {Report.UserId ?? "unknown user"}";

    /// <summary>
    /// Gets how the exception was caught, with a fatal marker.
    /// </summary>
    public string SourceText => Report.IsFatal ? Report.Source + " (fatal)" : Report.Source;
}
