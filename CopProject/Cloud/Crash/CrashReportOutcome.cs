namespace Cloud.Crash;

/// <summary>
/// What happened to a crash report handed to <see cref="CrashHandler"/>.
/// </summary>
public enum CrashReportOutcome
{
    /// <summary>The report reached the cloud.</summary>
    Uploaded,

    /// <summary>The upload failed or timed out, so the report waits in the local pending folder.</summary>
    SavedLocally,

    /// <summary>The report could be neither uploaded nor saved.</summary>
    Failed,

    /// <summary>The report was ignored: the handler is disposed, already reporting, or the session limit was reached.</summary>
    Skipped
}
