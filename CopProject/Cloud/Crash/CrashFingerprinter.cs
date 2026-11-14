using System.Security.Cryptography;
using System.Text;

namespace Cloud.Crash;

/// <summary>
/// Computes the fingerprint and classification of a crash. Pure and deterministic, so it can run on any machine or on Azure.
/// </summary>
public static class CrashFingerprinter
{
    /// <summary>
    /// Version of the fingerprint algorithm. It is part of the hash input, so changing the algorithm gives new fingerprints.
    /// </summary>
    public const int FingerprintVersion = 1;

    /// <summary>
    /// The number of frames that take part in the fingerprint.
    /// </summary>
    public const int MaxFrames = 5;

    /// <summary>
    /// The exception type used for reports that contain no exception.
    /// </summary>
    public const string UnknownExceptionType = "Unknown";

    /// <summary>
    /// The top frame shown for reports that contain no stack frames.
    /// </summary>
    public const string NoStackFrame = "(no stack)";

    private const int HashBytesUsed = 8;

    /// <summary>
    /// Fingerprints and classifies a crash report.
    /// </summary>
    /// <param name="report">The report to analyse.</param>
    /// <returns>The signature of the crash.</returns>
    public static CrashSignature Analyze(CrashReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        CrashExceptionInfo? root = GetRootCause(report);
        string exceptionType = root is null || string.IsNullOrWhiteSpace(root.Type) ? UnknownExceptionType : root.Type;
        List<CrashFrame> frames = root?.Frames ?? [];

        List<CrashFrame> appFrames = frames.Where(frame => ModuleCatalog.TryGetModule(frame.Assembly, out _)).ToList();
        List<CrashFrame> selected = appFrames.Count > 0 ? appFrames : frames;

        string module = appFrames.Count > 0 && ModuleCatalog.TryGetModule(appFrames[0].Assembly, out string found)
            ? found
            : ModuleCatalog.UnknownModule;

        List<string> names = NormalizeTopFrames(selected);
        string input = $"v{FingerprintVersion}|{exceptionType}|{string.Join('|', names)}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        string fingerprint = Convert.ToHexString(hash.AsSpan(0, HashBytesUsed)).ToLowerInvariant();

        return new CrashSignature(
            fingerprint,
            exceptionType,
            names.Count > 0 ? names[0] : NoStackFrame,
            module,
            CrashCategorizer.Categorize(exceptionType));
    }

    /// <summary>
    /// Finds the exception that actually caused the crash: the innermost exception that has a stack trace.
    /// Wrappers such as <see cref="AggregateException"/> are skipped this way.
    /// </summary>
    /// <param name="report">The report to inspect.</param>
    /// <returns>The root-cause exception, or null when the report has none.</returns>
    public static CrashExceptionInfo? GetRootCause(CrashReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        for (int i = report.Exceptions.Count - 1; i >= 0; i--)
        {
            if (report.Exceptions[i].Frames.Count > 0)
            {
                return report.Exceptions[i];
            }
        }

        return report.Exceptions.Count > 0 ? report.Exceptions[^1] : null;
    }

    private static List<string> NormalizeTopFrames(List<CrashFrame> frames)
    {
        List<string> names = [];

        foreach (CrashFrame frame in frames)
        {
            string name = StackNormalizer.NormalizeFrame(frame);

            // Recursion repeats the same frame many times; keep one so the depth does not change the fingerprint.
            if (names.Count > 0 && names[^1] == name)
            {
                continue;
            }

            names.Add(name);

            if (names.Count == MaxFrames)
            {
                break;
            }
        }

        return names;
    }
}
