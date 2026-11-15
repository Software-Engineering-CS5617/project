// Authors:
// Adwaith Nath R - Roll Number: 112614010
// Hariram S - Roll Number: 112614008

namespace Insights.AI;

public interface IInsightsProvider
{
    string ModelName { get; }

    Task<string> AnalyzeTrafficIncidentAsync(string textDescription);

    Task<string> AnalyzeImageAsync(byte[] imageData, string prompt);

    Task<string> GenerateChatResponseAsync(
        string userMessage,
        string? chatContext = null);
}
