using System;
using System.IO;
using System.Security;

namespace Filesync;

/// <summary>
/// Provides local file operations for the synchronised root directory.
/// Networking and remote synchronisation will be implemented separately.
/// </summary>
public class Synchroniser : ISync, IFileOperations
{
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
    /// Saves a new file in the synchronised root directory.
    /// </summary>
    public bool SaveFile(string relativePath, byte[] content)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(content);

            string fullPath = GetSafePath(relativePath);

            string? parentDirectory = Path.GetDirectoryName(fullPath);

            if (parentDirectory != null)
            {
                Directory.CreateDirectory(parentDirectory);
            }

            // Create New prevents overwriting an existing file.
            using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            stream.Write(content, 0, content.Length);

            return true;
        }
        catch (Exception ex) when (IsExpectedFileError(ex))
        {
            ReportError(ex);
            return false;
        }
    }

    /// <summary>
    /// Reads a file from the synchronised root directory
    /// </summary>
    /// <exception cref="IOException"> Thrown when the file cannot be read</exception>
    public byte[] ReadFile(string relativePath)
    {
        try
        {
            string fullPath = GetSafePath(relativePath);
            return File.ReadAllBytes(fullPath);
        }
        catch (Exception ex) when (IsExpectedFileError(ex))
        {
            ReportError(ex);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing file in the synchronised root directory.
    /// </summary>
    public bool UpdateFile(string relativePath, byte[] content)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(content);

            string fullPath = GetSafePath(relativePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The file does not exist.", fullPath);
            }

            File.WriteAllBytes(fullPath, content);

            return true;
        }
        catch (Exception ex) when (IsExpectedFileError(ex))
        {
            ReportError(ex);
            return false;
        }
    }

    /// <summary>
    /// Deletes a file from the synchronised root directory.
    /// </summary>
    public bool DeleteFile(string relativePath)
    {
        try
        {
            string fullPath = GetSafePath(relativePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The file does not exist.", fullPath);
            }

            File.Delete(fullPath);

            return true;
        }
        catch (Exception ex) when (IsExpectedFileError(ex))
        {
            ReportError(ex);
            return false;
        }
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
            // We get all files in the Root directory
            string[] allFiles = Directory.GetFiles(_rootDir, "*", SearchOption.AllDirectories);
            
            // Create a simple dictionary mapping the relative path to its timestamp
            var fileTimestamps = new System.Collections.Generic.Dictionary<string, DateTime>();
            
            foreach (string filePath in allFiles)
            {
                // We need the relative path so other computers understand it
                string relativePath = Path.GetRelativePath(_rootDir, filePath);
                
                // Get the exact time the file was saved (Last-Writer-Wins logic)
                DateTime lastModified = File.GetLastWriteTimeUtc(filePath);
                
                fileTimestamps[relativePath] = lastModified;
            }
            
            // TODO: Convert this dictionary to JSON and send it over the Networking module.
            // When the other computer receives it, it will compare its timestamps to ours
            // and send back any files that are newer.

            // Simulate that the sync finished successfully for the UI
            OnSyncComplete?.Invoke();
        }
        catch (Exception ex)
        {
            // If the network crashes or a file is locked, report to the UI
            OnSyncError?.Invoke(ex.Message);
        }
    }
}
