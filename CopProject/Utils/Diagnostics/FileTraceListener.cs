
using System.Diagnostics;

namespace Utils.Diagnostics;

/// <summary>
/// Writes Trace messages from this process to a file for troubleshooting.
/// </summary>
public static class FileTraceListener
{
    private static string? _logPath;
    private static string? _logDirectory;
    private static string? _logPathForReading;
    private static TextWriterTraceListener? _listener;

    /// <summary>
    /// Initializes the file trace listener.
    /// </summary>
    public static void Initialize()
    {
        /* The log file is created in C:\Users\<username>\AppData\Local\PawPatrol */
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "PawPatrol");

        Directory.CreateDirectory(_logDirectory);

        _logPath = Path.Combine(
            _logDirectory,
            $"PawPatrol-{Guid.NewGuid():N}.txt");


        var fileStream = new FileStream(
            _logPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite); /* permits simulaneous reading and writing of the log file.*/

        _listener = new TextWriterTraceListener(fileStream);

        Trace.Listeners.Add(_listener);
        Trace.AutoFlush = true;

        Trace.TraceInformation(
            $"Application started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    /// <summary>
    /// Reads the most recent application log entries.
    /// </summary>
    public static async Task<string> ReadRecentLogAsync(int lineCount = 100)
    {
        if (_logPath is null || _logDirectory is null)
        {
            Trace.TraceError(
                "FileTraceListener has not been initialized.");
            throw new InvalidOperationException(
                "FileTraceListener has not been initialized.");
        }

        Trace.Flush(); /* Ensure all log entries are written to the file from the buffer before reading.*/

        _logPathForReading = Path.Combine(
            _logDirectory,
            $"{Path.GetFileNameWithoutExtension(_logPath)}-read.txt");

        /* Create a separate snapshot of the original log.*/
        File.Copy(_logPath, _logPathForReading, true);

        /* Read the snapshot, not the original log file.*/
        string[] lines = await File.ReadAllLinesAsync(_logPathForReading);

        var recentLines = lines.TakeLast(lineCount);

        Trace.TraceInformation(
            $"Reading the last {lineCount} lines from the log snapshot.");

        return string.Join(Environment.NewLine, recentLines);
    }
}
