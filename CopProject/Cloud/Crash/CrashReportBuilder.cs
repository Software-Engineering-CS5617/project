using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Cloud.Crash;

/// <summary>
/// Builds a <see cref="CrashReport"/> from a live exception.
/// </summary>
public static class CrashReportBuilder
{
    /// <summary>
    /// The largest number of stack frames kept per exception.
    /// </summary>
    public const int MaxFramesPerException = 200;

    /// <summary>
    /// The longest exception chain that is captured.
    /// </summary>
    public const int MaxChainLength = 10;

    private const int MaxMessageLength = 2000;
    private const int MaxFullTextLength = 30000;

    /// <summary>
    /// Captures an exception and its inner exceptions. Text is cleaned so that it can be written as XML and
    /// does not contain user profile names.
    /// </summary>
    /// <param name="exception">The exception to capture.</param>
    /// <param name="isFatal">Whether the process is terminating because of the exception.</param>
    /// <param name="source">The hook that caught the exception.</param>
    /// <param name="userId">The ID of the current user, if known.</param>
    /// <returns>The report.</returns>
    public static CrashReport Build(Exception exception, bool isFatal, string source, string? userId = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Trace.TraceInformation($"CrashReportBuilder.Build: building report for {exception.GetType().Name}; fatal={isFatal}, source={source}.");

        CrashReport report = new() {
            IsFatal = isFatal,
            Source = CrashText.Sanitize(source),
            UserId = userId is null ? null : CrashText.Sanitize(userId),
            AppVersion = GetAppVersion(),
            OsDescription = CrashText.Sanitize(RuntimeInformation.OSDescription),
            FullText = Clean(Safe(exception.ToString), MaxFullTextLength)
        };

        Exception? current = exception;

        for (int depth = 0; current is not null && depth < MaxChainLength; depth++)
        {
            report.Exceptions.Add(Describe(current));
            current = current.InnerException;
        }

        Trace.TraceInformation($"CrashReportBuilder.Build: captured {report.Exceptions.Count} exception levels.");
        return report;
    }

    private static CrashExceptionInfo Describe(Exception exception)
    {
        Type type = exception.GetType();

        CrashExceptionInfo info = new() {
            Type = type.FullName ?? type.Name,
            Message = Clean(Safe(() => exception.Message), MaxMessageLength)
        };

        StackFrame[] frames = new StackTrace(exception, true).GetFrames();

        foreach (StackFrame frame in frames.Take(MaxFramesPerException))
        {
            MethodBase? method = frame.GetMethod();

            if (method is null)
            {
                continue;
            }

            Type? declaring = method.DeclaringType;
            Type? definition = declaring is { IsGenericType: true } ? declaring.GetGenericTypeDefinition() : declaring;

            info.Frames.Add(new CrashFrame {
                DeclaringType = definition is null ? string.Empty : definition.FullName ?? definition.Name,
                Method = method.Name,
                Assembly = method.Module.Assembly.GetName().Name ?? string.Empty,
                FileName = Path.GetFileName(frame.GetFileName()),
                Line = frame.GetFileLineNumber()
            });
        }

        return info;
    }

    private static string GetAppVersion()
    {
        string? version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        return string.IsNullOrEmpty(version) ? "unknown" : version;
    }

    private static string Clean(string text, int maxLength)
    {
        return CrashText.Truncate(CrashText.ScrubPaths(CrashText.Sanitize(text)), maxLength);
    }

    private static string Safe(Func<string?> getter)
    {
        try
        {
            return getter() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"CrashReportBuilder.Safe: exception text was unavailable: {ex.Message}");
            return "(unavailable: " + ex.GetType().Name + ")";
        }
    }
}
