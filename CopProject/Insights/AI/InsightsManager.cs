// Authors:
// Adwaith Nath R - Roll Number: 112614010
// Hariram S - Roll Number: 112614008

using System.Diagnostics;
using System.Text;
using Insights.LogAnalysis;

namespace Insights.AI;

public class InsightsManager
{
    private IInsightsProvider _activeProvider;

    public InsightsManager(IInsightsProvider defaultProvider)
    {
        ArgumentNullException.ThrowIfNull(defaultProvider);

        _activeProvider = defaultProvider;

        Trace.TraceInformation(
            "[Insights] InsightsManager initialized with provider '{0}'.",
            defaultProvider?.GetType().Name ?? "null");
    }

    public void SwitchModel(IInsightsProvider newProvider)
    {
        _activeProvider = newProvider
            ?? throw new ArgumentNullException(nameof(newProvider));

        Trace.TraceInformation(
            "[Insights] Switching active provider to '{0}'.",
            newProvider.GetType().Name);
    }

    public async Task<string> RequestTextAnalysisAsync(string text)
    {
        Trace.TraceInformation("[Insights] Text analysis request started.");

        try
        {
            string result = await _activeProvider.AnalyzeTrafficIncidentAsync(text);
            Trace.TraceInformation("[Insights] Text analysis request completed.");
            return result;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] Text analysis request failed: {0}", ex);
            throw;
        }
    }

    public async Task<string> RequestImageAnalysisAsync(
        byte[] image,
        string prompt)
    {
        Trace.TraceInformation("[Insights] Image analysis request started.");

        try
        {
            string result = await _activeProvider.AnalyzeImageAsync(image, prompt);
            Trace.TraceInformation("[Insights] Image analysis request completed.");
            return result;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] Image analysis request failed: {0}", ex);
            throw;
        }
    }

    public async Task<string> GenerateChatResponseAsync(
        string userMessage,
        string? chatContext = null)
    {
        Trace.TraceInformation("[Insights] Chat response request started.");

        try
        {
            string result = await _activeProvider.GenerateChatResponseAsync(
                userMessage,
                chatContext);

            Trace.TraceInformation("[Insights] Chat response request completed.");
            return result;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] Chat response request failed: {0}", ex);
            throw;
        }
    }

    public async Task<string> DiagnoseLogAsync(string log)
    {
        Trace.TraceInformation("[Insights] Log diagnosis request started.");

        if (string.IsNullOrWhiteSpace(log))
        {
            Trace.TraceWarning("[Insights] Log diagnosis rejected because the log content is empty.");
            throw new ArgumentException("Log content is required.", nameof(log));
        }

        if (_activeProvider is not ILogAnalysisProvider logProvider)
        {
            string providerName = _activeProvider.GetType().Name;

            Trace.TraceError(
                "[Insights] Active provider '{0}' does not support log analysis.",
                providerName);

            throw new InvalidOperationException(
                $"The active provider '{providerName}' does not support log analysis.");
        }

        try
        {
            GeminiLogAnalysisService logAnalysisService =
            GeminiLogAnalysisService.CreateWithProvider(logProvider);

            LogAnalysisResult result =
                await logAnalysisService.AnalyzeLogAsync(
                    new LogAnalysisRequest(log));

            string diagnosis = FormatLogAnalysisResult(result);

            Trace.TraceInformation("[Insights] Log diagnosis request completed.");

            return diagnosis;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] Log diagnosis request failed: {0}", ex);
            throw;
        }
    }

    private static string FormatLogAnalysisResult(LogAnalysisResult result)
    {
        StringBuilder output = new();

        string failureStatus = result.FailureDetected ? "Yes" : "No";

        output.AppendLine("Log Diagnosis");
        output.AppendLine();
        output.AppendLine($"Summary: {result.Summary}");
        output.AppendLine($"Failure detected: {failureStatus}");

        if (!string.IsNullOrWhiteSpace(result.FailureLocation))
        {
            output.AppendLine($"Failure location: {result.FailureLocation}");
        }

        if (!string.IsNullOrWhiteSpace(result.LikelyCause))
        {
            output.AppendLine($"Likely cause: {result.LikelyCause}");
        }

        output.AppendLine();
        output.AppendLine("Recommendations:");

        if (result.Recommendations.Count == 0)
        {
            output.AppendLine("- No recommendations provided.");
        }
        else
        {
            foreach (string recommendation in result.Recommendations)
            {
                if (!string.IsNullOrWhiteSpace(recommendation))
                {
                    output.AppendLine($"- {recommendation}");
                }
            }
        }

        return output.ToString().TrimEnd();
    }
}
