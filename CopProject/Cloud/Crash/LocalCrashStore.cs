using System.Diagnostics;

namespace Cloud.Crash;

/// <summary>
/// Keeps crash reports that could not be uploaded as XML files in a local folder.
/// </summary>
public sealed class LocalCrashStore
{
    /// <summary>
    /// The most reports kept on disk; older ones are deleted first.
    /// </summary>
    public const int MaxPendingReports = 100;

    /// <summary>
    /// Creates a store in the default folder under the user's local application data.
    /// </summary>
    public LocalCrashStore()
        : this(GetDefaultDirectory())
    {
    }

    /// <summary>
    /// Creates a store in the given folder. The folder is created when the first report is saved.
    /// </summary>
    /// <param name="directoryPath">The folder that holds pending reports.</param>
    public LocalCrashStore(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        DirectoryPath = Path.GetFullPath(directoryPath);
    }

    /// <summary>
    /// Gets the folder that holds pending reports.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Gets the number of reports waiting for upload.
    /// </summary>
    public int PendingCount => GetPendingFiles().Count;

    /// <summary>
    /// Gets the default folder for pending reports.
    /// </summary>
    /// <returns>The folder path.</returns>
    public static string GetDefaultDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CopProject",
            "Crashes",
            "pending");
    }

    /// <summary>
    /// Writes a report to disk. The file appears atomically, so a crash while saving never leaves a half-written report.
    /// </summary>
    /// <param name="report">The report, used for the file name.</param>
    /// <param name="xml">The serialized report.</param>
    /// <returns>The path of the saved file.</returns>
    public string Save(CrashReport report, byte[] xml)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(xml);

        Directory.CreateDirectory(DirectoryPath);

        string name = $"{report.TimestampUtc.ToUniversalTime():yyyyMMddTHHmmssfffZ}_{report.Id:D}.xml";
        string finalPath = Path.Combine(DirectoryPath, name);
        string tempPath = finalPath + ".tmp";

        File.WriteAllBytes(tempPath, xml);
        File.Move(tempPath, finalPath, overwrite: true);

        Prune();
        Trace.TraceInformation($"LocalCrashStore.Save: saved report {report.Id}.");
        return finalPath;
    }

    /// <summary>
    /// Lists the pending report files, oldest first.
    /// </summary>
    /// <returns>The file paths.</returns>
    public IReadOnlyList<string> GetPendingFiles()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Trace.TraceInformation("LocalCrashStore.GetPendingFiles: pending directory does not exist.");
            return [];
        }

        return Directory.GetFiles(DirectoryPath, "*.xml")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Reads a pending report file.
    /// </summary>
    /// <param name="path">A path returned by <see cref="GetPendingFiles"/>.</param>
    /// <returns>The file content.</returns>
    public byte[] Read(string path)
    {
        Trace.TraceInformation($"LocalCrashStore.Read: reading pending file '{path}'.");
        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Deletes a pending report file. A file that is already gone or locked is ignored.
    /// </summary>
    /// <param name="path">The file to delete.</param>
    public void Delete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"LocalCrashStore.Delete: could not delete {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Renames an unreadable report to <c>.bad</c> so it is no longer retried but is kept for inspection.
    /// </summary>
    /// <param name="path">The file to set aside.</param>
    public void Quarantine(string path)
    {
        try
        {
            File.Move(path, path + ".bad", overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"LocalCrashStore.Quarantine: could not quarantine {path}: {ex.Message}");
        }
    }

    private void Prune()
    {
        IReadOnlyList<string> files = GetPendingFiles();

        for (int i = 0; i < files.Count - MaxPendingReports; i++)
        {
            Delete(files[i]);
        }
    }
}
