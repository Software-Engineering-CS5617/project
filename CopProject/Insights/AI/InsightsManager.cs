using System.Diagnostics;

namespace Insights.AI;

public class InsightsManager
{
    private IInsightsProvider _activeProvider;

    public InsightsManager(IInsightsProvider defaultProvider)
    {
        _activeProvider = defaultProvider;

        Trace.TraceInformation(
            "[Insights] InsightsManager initialized with provider '{0}'.",
            defaultProvider?.GetType().Name ?? "null");
    }

    public void SwitchModel(IInsightsProvider newProvider)
    {
        Trace.TraceInformation(
            "[Insights] Switching active provider to '{0}'.",
            newProvider?.GetType().Name ?? "null");

        _activeProvider = newProvider;
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
}
