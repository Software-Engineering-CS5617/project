using Cloud.Crash;
using Cloud.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Cloud.Functions;

/// <summary>
/// Runs for every crash report uploaded to Cosmos: fingerprints it and counts it in its group.
/// </summary>
public sealed class CrashFingerprintFunction
{
    private static readonly SemaphoreSlim s_initLock = new(1, 1);
    private static CrashIngestionService? s_service;
    private static IStorage? s_reports;

    private readonly ILogger<CrashFingerprintFunction> _logger;

    /// <summary>
    /// Creates the function.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public CrashFingerprintFunction(ILogger<CrashFingerprintFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles a batch of new crash reports from the change feed.
    /// </summary>
    /// <param name="documents">The new documents of the CrashReports container.</param>
    /// <returns>A task that completes when the batch is processed.</returns>
    [Function("CrashFingerprint")]
    public async Task RunAsync(
        [CosmosDBTrigger(
            databaseName: "PersistenceDB",
            containerName: "CrashReports",
            Connection = "CosmosConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<CrashReportDocument> documents)
    {
        CrashIngestionService service = await GetServiceAsync();
        IStorage reports = s_reports!;
        int conflicts = 0;

        foreach (CrashReportDocument document in documents)
        {
            (bool found, byte[] xml) = await reports.Read(document.Id);
            CrashProcessResult result = found ? await service.ProcessAsync(xml) : CrashProcessResult.Invalid;

            if (result == CrashProcessResult.Invalid)
            {
                _logger.LogWarning("Crash report {Id} is not valid XML and was skipped.", document.Id);
            }
            else if (result == CrashProcessResult.Conflict)
            {
                conflicts++;
            }
        }

        if (conflicts > 0)
        {
            // Failing the batch makes the host retry it; reports already counted are recognised as duplicates.
            throw new InvalidOperationException($"{conflicts} crash report(s) could not be recorded; the batch will be retried.");
        }
    }

    private static async Task<CrashIngestionService> GetServiceAsync()
    {
        if (s_service is not null)
        {
            return s_service;
        }

        await s_initLock.WaitAsync();

        try
        {
            s_reports ??= await StorageFactory.CreateAsync(ModuleType.CrashReports);
            s_service ??= new CrashIngestionService(await StorageFactory.CreateAsync(ModuleType.CrashGroups));
            return s_service;
        }
        finally
        {
            s_initLock.Release();
        }
    }
}
