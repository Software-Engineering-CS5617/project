// Author: Adwaith Nath R
// Roll Number: 112614010

namespace Insights.LogAnalysis;

/// <summary>
/// Analyses application log content and returns a concise diagnostic summary.
/// </summary>
public interface ILogAnalysisService
{
    Task<LogAnalysisResult> AnalyzeLogAsync(LogAnalysisRequest request);
}
