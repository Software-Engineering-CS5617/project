// Author : Murali

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace FileSynchroniser.Models;

/// <summary>
/// Provides local file operations for the synchronised root directory.
/// Networking and remote synchronisation will be implemented separately.
/// </summary>
public class Synchroniser : ISync, IFileOperations
{
    /// <summary>
    /// Maximum number of files accepted in one call.
    /// </summary>
    public const int MaxBatchFileCount = 500;

    /// <summary>
    /// Maximum combined size (in bytes) accepted in one call (256 MB).
    /// Larger files should go through FileChunker before being saved.
    /// </summary>
    public const long MaxBatchTotalBytes = 256L * 1024 * 1024;

    private readonly string _rootDir;

    public event Action OnSyncComplete = delegate { };
    public event Action<string> OnSyncError = delegate { };

    public Synchroniser()
    {
        _rootDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FileSync", "Root"));

        Directory.CreateDirectory(_rootDir);
    }

    /// <summary>
    /// Returns the path to the synchronised root directory.
    /// </summary>
    public string GetDirectory()
    {
        return _rootDir;
    }

    /// <summary>
    /// Saves one or more new files in the synchronised root directory.
    /// Existing files are never overwritten.
    /// </summary>
    public IReadOnlyDictionary<string, bool> SaveFile(IReadOnlyDictionary<string, byte[]> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        return ExecuteBatch(
            files.Keys.ToList(),
            files.Values.Sum(v => v?.LongLength ?? 0),
            path => {
                byte[] content = files[path];
                ArgumentNullException.ThrowIfNull(content);

                string fullPath = GetSafePath(path);

                string? parentDirectory = Path.GetDirectoryName(fullPath);

                if (parentDirectory != null)
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                // Create New prevents overwriting an existing file.
                using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

                stream.Write(content, 0, content.Length);

                return true;
            });
    }

    /// <summary>
    /// Reads one or more files from the synchronised root directory.
    /// A file that cannot be read maps to null.
    /// </summary>
    public IReadOnlyDictionary<string, byte[]?> ReadFile(IEnumerable<string> relativePaths)
    {
        ArgumentNullException.ThrowIfNull(relativePaths);

        long totalBytesRead = 0;

        return ExecuteBatch<byte[]?>(
            relativePaths.ToList(),
            0,
            path => {
                string fullPath = GetSafePath(path);

                // Enforce the total size cap before loading the file into memory.
                long length = new FileInfo(fullPath).Length;

                if (totalBytesRead + length > MaxBatchTotalBytes)
                {
                    throw new IOException($"Batch size limit of {MaxBatchTotalBytes} bytes exceeded.");
                }

                byte[] data = File.ReadAllBytes(fullPath);
                totalBytesRead += data.LongLength;
                return data;
            });
    }

    /// <summary>
    /// Updates one or more existing files in the synchronised root directory.
    /// </summary>
    public IReadOnlyDictionary<string, bool> UpdateFile(IReadOnlyDictionary<string, byte[]> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        return ExecuteBatch(
            files.Keys.ToList(),
            files.Values.Sum(v => v?.LongLength ?? 0),
            path => {
                byte[] content = files[path];
                ArgumentNullException.ThrowIfNull(content);

                string fullPath = GetSafePath(path);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("The file does not exist.", fullPath);
                }

                File.WriteAllBytes(fullPath, content);

                return true;
            });
    }

    /// <summary>
    /// Deletes one or more files from the synchronised root directory.
    /// </summary>
    public IReadOnlyDictionary<string, bool> DeleteFile(IEnumerable<string> relativePaths)
    {
        ArgumentNullException.ThrowIfNull(relativePaths);

        return ExecuteBatch(
            relativePaths.ToList(),
            0,
            path => {
                string fullPath = GetSafePath(path);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("The file does not exist.", fullPath);
                }

                File.Delete(fullPath);

                return true;
            });
    }

    /// <summary>
    /// Checks whether a file exists in the synchronised root directory.
    /// </summary>
    public bool FileExists(string relativePath)
    {
        try
        {
            string fullPath = GetSafePath(relativePath);
            return File.Exists(fullPath);
        }
        catch (Exception ex) when (IsExpectedFileError(ex))
        {
            ReportError(ex);
            return false;
        }
    }

    /// <summary>
    /// Runs an operation for every path, collecting per-file results and
    /// raising a single OnSyncError summary event at the end if any failed.
    /// Failed entries map to default(T) (false for bool, null for byte[]?).
    /// </summary>
    private Dictionary<string, T> ExecuteBatch<T>(
        List<string> paths,
        long totalBytes,
        Func<string, T> operation)
    {
        var results = new Dictionary<string, T>(StringComparer.Ordinal);
        var errors = new List<string>();

        // Reject oversized batches up front: every entry is reported as failed.
        if (paths.Count > MaxBatchFileCount)
        {
            FailAll(paths, results, $"Batch contains {paths.Count} files; the limit is {MaxBatchFileCount}.");
            return results;
        }

        if (totalBytes > MaxBatchTotalBytes)
        {
            FailAll(paths, results, $"Batch size of {totalBytes} bytes exceeds the limit of {MaxBatchTotalBytes} bytes.");
            return results;
        }

        // Tracks resolved paths so that "A.txt" and "a.txt" (or "x/../a.txt")
        // in the same batch do not silently clobber each other.
        var seenFullPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            if (path == null)
            {
                errors.Add("A null path was skipped.");
                continue;
            }

            if (results.ContainsKey(path))
            {
                continue; // exact duplicate of an entry already processed
            }

            try
            {
                string fullPath = GetSafePath(path);

                if (!seenFullPaths.Add(fullPath))
                {
                    throw new ArgumentException("Duplicate path in batch.", nameof(paths));
                }

                results[path] = operation(path);
            }
            catch (Exception ex) when (IsExpectedFileError(ex))
            {
                results[path] = default!;
                errors.Add($"{path}: {ex.Message}");
            }
        }

        ReportBatchErrors(errors);
        return results;
    }

    private void FailAll<T>(List<string> paths, Dictionary<string, T> results, string message)
    {
        foreach (string path in paths.Where(p => p != null))
        {
            results[path] = default!;
        }

        OnSyncError(message);
    }

    private void ReportBatchErrors(List<string> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        OnSyncError($"{errors.Count} file operation(s) failed: {string.Join("; ", errors)}");
    }

    /// <summary>
    /// Resolves a relative path and prevents access outside Root.
    /// </summary>
    private string GetSafePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("The relative path cannot be empty.", nameof(relativePath));
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Absolute paths are not allowed.", nameof(relativePath));
        }

        string fullPath = Path.GetFullPath(Path.Combine(_rootDir, relativePath));

        string rootPrefix = _rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The path must remain inside the synchronised root directory.", nameof(relativePath));
        }

        return fullPath;
    }

    /// <summary>
    /// All File related Errors
    /// </summary>
    private static bool IsExpectedFileError(Exception ex)
    {
        return ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException
            or SecurityException;
    }

    /// <summary>
    /// Reports the error catched
    /// </summary>
    private void ReportError(Exception ex)
    {
        OnSyncError(ex.Message);
    }

    /// <summary>
    /// Synchronises
    /// </summary>
    public void Synchronise()
    {
        try
        {
            string[] allFiles = Directory.GetFiles(_rootDir, "*", SearchOption.AllDirectories);

            var fileTimestamps = new Dictionary<string, DateTime>();

            foreach (string filePath in allFiles)
            {
                string relativePath = Path.GetRelativePath(_rootDir, filePath);
                DateTime lastModified = File.GetLastWriteTimeUtc(filePath);

                fileTimestamps[relativePath] = lastModified;
            }

            OnSyncComplete?.Invoke();
        }
        catch (Exception ex)
        {
            OnSyncError?.Invoke(ex.Message);
        }
    }
}
