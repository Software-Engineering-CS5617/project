namespace Cloud.Crash;

/// <summary>
/// The result of analysing a crash: which group it belongs to and how it is classified.
/// </summary>
/// <param name="Fingerprint">Stable hash that identifies the group of identical crashes.</param>
/// <param name="ExceptionType">Full type name of the root-cause exception.</param>
/// <param name="TopFrame">Normalized name of the top stack frame.</param>
/// <param name="Module">Name of the module that owns the top application frame.</param>
/// <param name="Category">Broad kind of failure.</param>
public sealed record CrashSignature(
    string Fingerprint,
    string ExceptionType,
    string TopFrame,
    string Module,
    CrashCategory Category);
