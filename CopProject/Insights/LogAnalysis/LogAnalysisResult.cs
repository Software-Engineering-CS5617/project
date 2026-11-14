namespace Insights.LogAnalysis;

/// <summary>
/// Structured findings from analysing an application log.
/// FailureLocation should only identify a location supported by the supplied log evidence.
/// </summary>
public sealed record LogAnalysisResult(
    string Summary,
    bool FailureDetected,
    string? FailureLocation,
    string? LikelyCause,
    IReadOnlyList<string> Recommendations);
