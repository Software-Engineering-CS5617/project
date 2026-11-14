using System.Xml.Serialization;

namespace Cloud.Crash;

/// <summary>
/// All crashes that share one fingerprint, with a running count.
/// </summary>
[XmlRoot("CrashGroup")]
public sealed class CrashGroup
{
    /// <summary>
    /// The number of recent report IDs a group remembers.
    /// </summary>
    public const int MaxRecentReports = 10;

    private const int MaxSampleMessageLength = 500;

    /// <summary>
    /// Gets or sets the version of the XML schema this group was written with.
    /// </summary>
    [XmlAttribute("schemaVersion")]
    public int SchemaVersion { get; set; } = CrashReport.CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets the fingerprint shared by every crash in the group.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the broad kind of failure.
    /// </summary>
    public CrashCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the module that owns the top application frame.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full type name of the root-cause exception.
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized name of the top stack frame.
    /// </summary>
    public string TopFrame { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message of the first crash seen, shortened.
    /// </summary>
    public string SampleMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how many crashes the group holds.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets when the earliest crash happened.
    /// </summary>
    public DateTime FirstSeenUtc { get; set; }

    /// <summary>
    /// Gets or sets when the latest crash happened.
    /// </summary>
    public DateTime LastSeenUtc { get; set; }

    /// <summary>
    /// Gets or sets the IDs of the most recent reports, oldest first.
    /// </summary>
    [XmlArray("RecentReportIds")]
    [XmlArrayItem("Id")]
    public List<Guid> RecentReportIds { get; set; } = [];

    /// <summary>
    /// Creates a new group that holds a single crash.
    /// </summary>
    /// <param name="signature">The signature computed for the report.</param>
    /// <param name="report">The first report of the group.</param>
    /// <returns>A group with a count of one.</returns>
    public static CrashGroup FromReport(CrashSignature signature, CrashReport report)
    {
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(report);

        DateTime seen = report.TimestampUtc.ToUniversalTime();
        CrashExceptionInfo? root = CrashFingerprinter.GetRootCause(report);

        CrashGroup group = new() {
            Fingerprint = signature.Fingerprint,
            Category = signature.Category,
            Module = signature.Module,
            ExceptionType = signature.ExceptionType,
            TopFrame = signature.TopFrame,
            SampleMessage = CrashText.Truncate(root?.Message, MaxSampleMessageLength),
            FirstSeenUtc = seen,
            LastSeenUtc = seen
        };

        group.Record(report);
        return group;
    }

    /// <summary>
    /// Adds a crash to the group. A report that is already among the recent IDs is ignored, so
    /// at-least-once delivery from the change feed does not double count.
    /// </summary>
    /// <param name="report">The report to add.</param>
    /// <returns><see langword="true"/> if the report was counted, <see langword="false"/> if it was a duplicate.</returns>
    public bool Record(CrashReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (RecentReportIds.Contains(report.Id))
        {
            return false;
        }

        DateTime seen = report.TimestampUtc.ToUniversalTime();

        Count++;
        FirstSeenUtc = Count == 1 ? seen : (seen < FirstSeenUtc ? seen : FirstSeenUtc);
        LastSeenUtc = seen > LastSeenUtc ? seen : LastSeenUtc;
        RecentReportIds.Add(report.Id);

        while (RecentReportIds.Count > MaxRecentReports)
        {
            RecentReportIds.RemoveAt(0);
        }

        return true;
    }
}
