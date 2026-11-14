using System.Diagnostics;
using Cloud.Persistence;

namespace Cloud.Crash;

/// <summary>
/// Uploads crash reports to the CrashReports container through <see cref="IStorage"/>.
/// </summary>
public sealed class CosmosCrashUploader : ICrashUploader
{
    private readonly Func<Task<IStorage>> _storageFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IStorage? _storage;

    /// <summary>
    /// Creates an uploader that talks to the real CrashReports container.
    /// </summary>
    public CosmosCrashUploader()
        : this(() => StorageFactory.CreateAsync(ModuleType.CrashReports))
    {
    }

    /// <summary>
    /// Creates an uploader that gets its storage from a factory, which is called lazily and retried after failures.
    /// </summary>
    /// <param name="storageFactory">Creates the storage for crash reports.</param>
    public CosmosCrashUploader(Func<Task<IStorage>> storageFactory)
    {
        ArgumentNullException.ThrowIfNull(storageFactory);
        _storageFactory = storageFactory;
    }

    /// <inheritdoc />
    public async Task<bool> UploadAsync(Guid reportId, byte[] reportXml, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reportXml);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            IStorage storage = await GetStorageAsync(cancellationToken).ConfigureAwait(false);
            string key = reportId.ToString("D");
            Trace.TraceInformation($"CosmosCrashUploader.UploadAsync: uploading report {reportId}.");

            if (await storage.Create(key, reportXml).ConfigureAwait(false))
            {
                Trace.TraceInformation($"CosmosCrashUploader.UploadAsync: report {reportId} created.");
                return true;
            }

            // Create fails for an existing key. That is fine: an earlier upload may have succeeded
            // before the local copy could be deleted.
            (bool found, _) = await storage.Read(key).ConfigureAwait(false);
            Trace.TraceWarning($"CosmosCrashUploader.UploadAsync: report {reportId} already existed={found}.");
            return found;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"CosmosCrashUploader.UploadAsync: upload of {reportId} failed: {ex.Message}");
            return false;
        }
    }

    private async Task<IStorage> GetStorageAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _storage ??= await _storageFactory().ConfigureAwait(false);
            Trace.TraceInformation("CosmosCrashUploader.GetStorageAsync: storage initialized or reused.");
            return _storage;
        }
        finally
        {
            _lock.Release();
        }
    }
}
