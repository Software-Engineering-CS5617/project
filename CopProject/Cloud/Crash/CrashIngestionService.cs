using System.Diagnostics;

namespace Cloud.Crash;

/// <summary>
/// Adds uploaded crash reports to their groups. This is what runs on Azure: the Function passes it each new report.
/// </summary>
public sealed class CrashIngestionService
{
    /// <summary>
    /// How many times a group update is retried when another writer got there first.
    /// </summary>
    public const int MaxAttempts = 5;

    private static readonly TimeSpan s_defaultRetryDelay = TimeSpan.FromMilliseconds(50);

    private readonly Persistence.IStorage _groups;
    private readonly TimeSpan _retryDelay;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="groups">The storage of the CrashGroups container.</param>
    /// <param name="retryDelay">The pause after a failed attempt, multiplied by the attempt number.</param>
    public CrashIngestionService(Persistence.IStorage groups, TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(groups);

        _groups = groups;
        _retryDelay = retryDelay ?? s_defaultRetryDelay;
    }

    /// <summary>
    /// Fingerprints one report and counts it in its group. Safe to call twice for the same report and
    /// safe with several instances running at once: updates use the ETag of the group document.
    /// </summary>
    /// <param name="reportXml">The serialized report.</param>
    /// <returns>What happened to the report.</returns>
    public async Task<CrashProcessResult> ProcessAsync(byte[] reportXml)
    {
        ArgumentNullException.ThrowIfNull(reportXml);
        Trace.TraceInformation($"CrashIngestionService.ProcessAsync: processing report payload of {reportXml.Length} bytes.");

        CrashReport report;

        try
        {
            report = CrashXmlSerializer.Deserialize<CrashReport>(reportXml);
        }
        catch (InvalidDataException ex)
        {
            Trace.TraceWarning($"CrashIngestionService.ProcessAsync: invalid report XML: {ex.Message}");
            return CrashProcessResult.Invalid;
        }

        CrashSignature signature = CrashFingerprinter.Analyze(report);
        string key = signature.Fingerprint;
        Trace.TraceInformation($"CrashIngestionService.ProcessAsync: using fingerprint '{key}'.");

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // Read the ETag before the data: if the group changes in between, the update below fails and we retry.
            (bool exists, string etag) = await _groups.ReadEtag(key).ConfigureAwait(false);
            Trace.TraceInformation($"CrashIngestionService.ProcessAsync: attempt {attempt}; groupExists={exists}.");

            if (!exists)
            {
                CrashGroup created = CrashGroup.FromReport(signature, report);

                if (await _groups.Create(key, CrashXmlSerializer.Serialize(created)).ConfigureAwait(false))
                {
                    Trace.TraceInformation("CrashIngestionService.ProcessAsync: created a new crash group.");
                    return CrashProcessResult.Recorded;
                }
            }
            else
            {
                (bool found, byte[] data) = await _groups.Read(key).ConfigureAwait(false);

                if (found)
                {
                    CrashGroup group = ReadGroupOrReplace(data, signature, report, out bool replaced);

                    if (!replaced && !group.Record(report))
                    {
                        Trace.TraceInformation("CrashIngestionService.ProcessAsync: duplicate report ignored.");
                        return CrashProcessResult.Duplicate;
                    }

                    if (await _groups.Update(key, CrashXmlSerializer.Serialize(group), etag).ConfigureAwait(false))
                    {
                        Trace.TraceInformation("CrashIngestionService.ProcessAsync: updated the crash group.");
                        return CrashProcessResult.Recorded;
                    }
                }
            }

            if (attempt < MaxAttempts)
            {
                await Task.Delay(_retryDelay * attempt).ConfigureAwait(false);
            }
        }

        Trace.TraceWarning("CrashIngestionService.ProcessAsync: group update conflicted after all retry attempts.");
        return CrashProcessResult.Conflict;
    }

    private static CrashGroup ReadGroupOrReplace(byte[] data, CrashSignature signature, CrashReport report, out bool replaced)
    {
        try
        {
            replaced = false;
            return CrashXmlSerializer.Deserialize<CrashGroup>(data);
        }
        catch (InvalidDataException ex)
        {
            // A corrupt group document would block this fingerprint forever, so start it again.
            Trace.TraceWarning($"CrashIngestionService.ReadGroupOrReplace: replacing corrupt group data: {ex.Message}");
            replaced = true;
            return CrashGroup.FromReport(signature, report);
        }
    }
}
