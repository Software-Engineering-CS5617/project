namespace Insights.LogAnalysis;

/// <summary>
/// Contains the text read from an application log file.
/// The caller is responsible for removing secrets and sensitive personal data before submission.
/// </summary>
public sealed record LogAnalysisRequest(string LogContent);
