using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cloud.Persistence;

namespace Cloud.Crash;

/// <summary>
/// Reads crash groups and reports from the CrashGroups and CrashReports containers.
/// </summary>
public sealed class CrashReportService : ICrashReportService
{
    private readonly IStorage _groups;
    private readonly IStorage _reports;
    private readonly CrashPendingUploader? _pending;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="groups">The storage of the CrashGroups container.</param>
    /// <param name="reports">The storage of the CrashReports container.</param>
    /// <param name="pending">The uploader for reports saved on this device, or null if none.</param>
    public CrashReportService(IStorage groups, IStorage reports, CrashPendingUploader? pending = null)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(reports);

        _groups = groups;
        _reports = reports;
        _pending = pending;
    }

    /// <summary>
    /// Creates the service on top of the real Cosmos containers.
    /// </summary>
    /// <param name="pending">The uploader for reports saved on this device, or null if none.</param>
    /// <returns>The service.</returns>
    public static async Task<CrashReportService> CreateAsync(CrashPendingUploader? pending = null)
    {
        Trace.TraceInformation("CrashReportService.CreateAsync: creating crash group and report storage.");
        IStorage groups = await StorageFactory.CreateAsync(ModuleType.CrashGroups).ConfigureAwait(false);
        IStorage reports = await StorageFactory.CreateAsync(ModuleType.CrashReports).ConfigureAwait(false);

        return new CrashReportService(groups, reports, pending);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CrashGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        Trace.TraceInformation("CrashReportService.GetGroupsAsync: loading crash groups.");
        (bool success, List<string> keys) = await _groups.GetAllKeys().ConfigureAwait(false);

        if (!success)
        {
            Trace.TraceError("CrashReportService.GetGroupsAsync: failed to list crash groups.");
            throw new InvalidOperationException("The crash groups could not be listed.");
        }

        List<CrashGroup> groups = [];

        foreach (string key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (bool found, byte[] data) = await _groups.Read(key).ConfigureAwait(false);

            if (found && TryDeserialize(data, out CrashGroup? group))
            {
                groups.Add(group);
            }
        }

        IReadOnlyList<CrashGroup> result = groups
            .OrderByDescending(group => group.LastSeenUtc.ToUniversalTime())
            .ThenByDescending(group => group.Count)
            .ToList();
        Trace.TraceInformation($"CrashReportService.GetGroupsAsync: loaded {result.Count} crash groups.");
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CrashReport>> GetReportsAsync(CrashGroup group, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        Trace.TraceInformation($"CrashReportService.GetReportsAsync: loading reports for group {group.Fingerprint}.");

        List<CrashReport> reports = [];

        foreach (Guid id in group.RecentReportIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (bool found, byte[] data) = await _reports.Read(id.ToString("D")).ConfigureAwait(false);

            if (found && TryDeserialize(data, out CrashReport? report))
            {
                reports.Add(report);
            }
        }

        IReadOnlyList<CrashReport> result = reports
            .OrderByDescending(report => report.TimestampUtc.ToUniversalTime())
            .ToList();
        Trace.TraceInformation($"CrashReportService.GetReportsAsync: loaded {result.Count} reports.");
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<CrashReport> GetPendingReports()
    {
        return _pending?.GetPendingReports() ?? [];
    }

    /// <inheritdoc />
    public int GetPendingCount()
    {
        return _pending?.PendingCount ?? 0;
    }

    /// <inheritdoc />
    public async Task<int> RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        if (_pending is null)
        {
            Trace.TraceWarning("CrashReportService.RetryPendingAsync: no pending uploader is configured.");
            return 0;
        }

        Trace.TraceInformation("CrashReportService.RetryPendingAsync: retrying pending reports.");
        return await _pending.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool TryDeserialize<T>(byte[] data, [NotNullWhen(true)] out T? value)
        where T : class
    {
        try
        {
            value = CrashXmlSerializer.Deserialize<T>(data);
            return true;
        }
        catch (InvalidDataException ex)
        {
            Trace.TraceWarning($"CrashReportService.TryDeserialize: skipped invalid crash data: {ex.Message}");
            value = null;
            return false;
        }
    }
}
