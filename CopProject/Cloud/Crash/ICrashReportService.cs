namespace Cloud.Crash;

/// <summary>
/// Reads crash groups and reports for the UI.
/// </summary>
public interface ICrashReportService
{
    /// <summary>
    /// Gets all crash groups, most recently seen first.
    /// </summary>
    /// <param name="cancellationToken">A token to stop the call.</param>
    /// <returns>The groups.</returns>
    Task<IReadOnlyList<CrashGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent reports of a group, newest first.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <param name="cancellationToken">A token to stop the call.</param>
    /// <returns>The reports that could still be read.</returns>
    Task<IReadOnlyList<CrashReport>> GetReportsAsync(CrashGroup group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the reports saved on this device that have not been uploaded, newest first.
    /// </summary>
    /// <returns>The pending reports.</returns>
    IReadOnlyList<CrashReport> GetPendingReports();

    /// <summary>
    /// Gets the number of reports saved on this device that have not been uploaded.
    /// </summary>
    /// <returns>The count.</returns>
    int GetPendingCount();

    /// <summary>
    /// Tries to upload the reports saved on this device.
    /// </summary>
    /// <param name="cancellationToken">A token to stop the call.</param>
    /// <returns>The number of reports uploaded.</returns>
    Task<int> RetryPendingAsync(CancellationToken cancellationToken = default);
}
