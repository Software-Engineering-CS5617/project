namespace Cloud.Crash;

/// <summary>
/// The result of adding one uploaded crash report to its group.
/// </summary>
public enum CrashProcessResult
{
    /// <summary>The report was counted in its group.</summary>
    Recorded,

    /// <summary>The report had already been counted.</summary>
    Duplicate,

    /// <summary>The report XML could not be read; retrying will not help.</summary>
    Invalid,

    /// <summary>Concurrent writers kept winning; the report should be retried later.</summary>
    Conflict
}
