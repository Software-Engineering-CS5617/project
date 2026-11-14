namespace Cloud.Crash;

/// <summary>
/// Sends crash reports to the cloud.
/// </summary>
public interface ICrashUploader
{
    /// <summary>
    /// Uploads one report. Uploading a report that is already in the cloud counts as success.
    /// </summary>
    /// <param name="reportId">The ID of the report.</param>
    /// <param name="reportXml">The serialized report.</param>
    /// <param name="cancellationToken">A token that is cancelled when the caller stops waiting.</param>
    /// <returns><see langword="true"/> if the report is in the cloud afterwards; never throws for network problems.</returns>
    Task<bool> UploadAsync(Guid reportId, byte[] reportXml, CancellationToken cancellationToken);
}
