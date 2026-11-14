using System.Diagnostics;

namespace Cloud.Crash;

/// <summary>
/// Uploads the reports waiting in a <see cref="LocalCrashStore"/>.
/// </summary>
public sealed class CrashPendingUploader
{
    private readonly LocalCrashStore _store;
    private readonly ICrashUploader _uploader;
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    /// <summary>
    /// Creates a pending uploader.
    /// </summary>
    /// <param name="store">The store that holds pending reports.</param>
    /// <param name="uploader">The uploader to send them with.</param>
    public CrashPendingUploader(LocalCrashStore store, ICrashUploader uploader)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(uploader);

        _store = store;
        _uploader = uploader;
    }

    /// <summary>
    /// Gets the number of reports waiting for upload.
    /// </summary>
    public int PendingCount => _store.PendingCount;

    /// <summary>
    /// Reads the reports waiting for upload. Unreadable files are skipped.
    /// </summary>
    /// <returns>The pending reports, newest first.</returns>
    public IReadOnlyList<CrashReport> GetPendingReports()
    {
        List<CrashReport> reports = [];
        Trace.TraceInformation("CrashPendingUploader.GetPendingReports: reading pending reports.");

        foreach (string path in _store.GetPendingFiles())
        {
            try
            {
                reports.Add(CrashXmlSerializer.Deserialize<CrashReport>(_store.Read(path)));
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                // Unreadable files are dealt with by the next flush.
                Trace.TraceWarning($"CrashPendingUploader.GetPendingReports: skipped unreadable file: {ex.Message}");
            }
        }

        return reports.OrderByDescending(report => report.TimestampUtc.ToUniversalTime()).ToList();
    }

    /// <summary>
    /// Uploads pending reports, oldest first, and deletes each one after it arrived. Stops at the first failed
    /// upload (the machine is probably offline) and leaves the rest for next time. Unreadable files are set aside.
    /// If another flush is already running this returns immediately.
    /// </summary>
    /// <param name="cancellationToken">A token to stop the flush.</param>
    /// <returns>The number of reports uploaded.</returns>
    public async Task<int> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!await _flushLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            Trace.TraceWarning("CrashPendingUploader.FlushAsync: another flush is already running.");
            return 0;
        }

        try
        {
            int uploaded = 0;
            Trace.TraceInformation("CrashPendingUploader.FlushAsync: starting pending upload.");

            foreach (string path in _store.GetPendingFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[] xml;
                CrashReport report;

                try
                {
                    xml = _store.Read(path);
                    report = CrashXmlSerializer.Deserialize<CrashReport>(xml);
                }
                catch (InvalidDataException)
                {
                    Trace.TraceWarning($"CrashPendingUploader.FlushAsync: quarantining invalid report file '{path}'.");
                    _store.Quarantine(path);
                    continue;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Trace.TraceWarning($"CrashPendingUploader.FlushAsync: could not read '{path}': {ex.Message}");
                    continue;
                }

                if (!await _uploader.UploadAsync(report.Id, xml, cancellationToken).ConfigureAwait(false))
                {
                    Trace.TraceWarning($"CrashPendingUploader.FlushAsync: upload failed for report {report.Id}; stopping the flush.");
                    break;
                }

                _store.Delete(path);
                uploaded++;
            }

            return uploaded;
        }
        finally
        {
            _flushLock.Release();
            Trace.TraceInformation("CrashPendingUploader.FlushAsync: pending upload finished.");
        }
    }
}
