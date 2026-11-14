namespace Cloud.Crash;

/// <summary>
/// Assigns a broad category to a crash from the type name of its root-cause exception.
/// </summary>
public static class CrashCategorizer
{
    /// <summary>
    /// Categorizes an exception by type name. Names are matched, not types, because reports come from other machines.
    /// </summary>
    /// <param name="exceptionType">The full or short exception type name.</param>
    /// <returns>The category, or <see cref="CrashCategory.Other"/> when unknown.</returns>
    public static CrashCategory Categorize(string? exceptionType)
    {
        if (string.IsNullOrWhiteSpace(exceptionType))
        {
            return CrashCategory.Other;
        }

        int lastDot = exceptionType.LastIndexOf('.');
        string shortName = lastDot < 0 ? exceptionType : exceptionType[(lastDot + 1)..];

        return shortName switch {
            "NullReferenceException" => CrashCategory.NullReference,
            "ArgumentException" or "ArgumentNullException" or "ArgumentOutOfRangeException" => CrashCategory.InvalidArgument,
            "InvalidOperationException" or "ObjectDisposedException" or "NotSupportedException" or "NotImplementedException" => CrashCategory.InvalidState,
            "IOException" or "FileNotFoundException" or "DirectoryNotFoundException" or "PathTooLongException"
                or "EndOfStreamException" or "FileLoadException" or "UnauthorizedAccessException" => CrashCategory.FileSystem,
            "SocketException" or "HttpRequestException" or "WebException" or "TimeoutException" => CrashCategory.Network,
            "CosmosException" => CrashCategory.Storage,
            "TaskCanceledException" or "OperationCanceledException" or "ThreadAbortException"
                or "SynchronizationLockException" => CrashCategory.Concurrency,
            "JsonException" or "XmlException" or "FormatException" or "InvalidDataException"
                or "InvalidCastException" or "SerializationException" => CrashCategory.DataFormat,
            "OutOfMemoryException" or "InsufficientMemoryException" or "StackOverflowException" => CrashCategory.Resources,
            "COMException" or "InvalidComObjectException" or "DllNotFoundException" or "BadImageFormatException" => CrashCategory.Platform,
            "IndexOutOfRangeException" or "KeyNotFoundException" or "DivideByZeroException"
                or "OverflowException" or "ArithmeticException" => CrashCategory.Logic,
            _ => CrashCategory.Other
        };
    }
}
