using System.Diagnostics.CodeAnalysis;
using Cloud.Crash;

namespace Cloud.UIharness;

/// <summary>
/// An in-memory <see cref="ICrashReportService"/> with a few made-up crashes, so the UI can be tried without Azure.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SampleCrashReportService : ICrashReportService
{
    private readonly List<CrashReport> _reports = [];
    private readonly List<CrashGroup> _groups = [];

    private readonly CrashPendingUploader _pending;

    private SampleCrashReportService(CrashPendingUploader pending)
    {
        _pending = pending;
    }

    /// <summary>
    /// Creates a service with sample groups; pending reports come from the real local folder.
    /// </summary>
    /// <param name="pending">The uploader of the real local pending reports.</param>
    /// <returns>The service.</returns>
    public static SampleCrashReportService Create(CrashPendingUploader pending)
    {
        SampleCrashReportService service = new(pending);
        service.Add("System.NullReferenceException", "Chat", "Chat.ChatServer", "HandleOperation", 4);
        service.Add("System.IO.IOException", "FileSynchroniser", "FileSynchroniser.FileWatcher", "Sync", 2);
        service.Add("System.Net.Sockets.SocketException", "Networking", "Networking.ReceivingQueueListener", "ListenOnQueue", 6);
        service.Add("System.InvalidOperationException", "Incident", "Incident_module.IncidentController", "CreateIncident", 1);
        return service;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CrashGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<CrashGroup>>(_groups.OrderByDescending(group => group.LastSeenUtc).ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CrashReport>> GetReportsAsync(CrashGroup group, CancellationToken cancellationToken = default)
    {
        List<CrashReport> reports = _reports
            .Where(report => group.RecentReportIds.Contains(report.Id))
            .OrderByDescending(report => report.TimestampUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<CrashReport>>(reports);
    }

    /// <inheritdoc />
    public IReadOnlyList<CrashReport> GetPendingReports()
    {
        return _pending.GetPendingReports();
    }

    /// <inheritdoc />
    public int GetPendingCount()
    {
        return _pending.PendingCount;
    }

    /// <inheritdoc />
    public Task<int> RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        return _pending.FlushAsync(cancellationToken);
    }

    private void Add(string exceptionType, string assembly, string type, string method, int times)
    {
        CrashGroup? group = null;

        for (int i = 0; i < times; i++)
        {
            CrashReport report = new() {
                TimestampUtc = DateTime.UtcNow.AddHours(-i * 5 - 1),
                AppVersion = "1.0." + i,
                OsDescription = "Windows 11",
                UserId = "user-" + i,
                IsFatal = i % 2 == 0,
                Source = "AppDomain",
                FullText = $"{exceptionType}: sample crash\r\n   at {type}.{method}()",
                Exceptions = [
                    new CrashExceptionInfo {
                        Type = exceptionType,
                        Message = "sample crash",
                        Frames = [new CrashFrame { DeclaringType = type, Method = method, Assembly = assembly, FileName = method + ".cs", Line = 10 + i }]
                    }
                ]
            };

            _reports.Add(report);

            if (group is null)
            {
                group = CrashGroup.FromReport(CrashFingerprinter.Analyze(report), report);
                _groups.Add(group);
            }
            else
            {
                group.Record(report);
            }
        }
    }
}
