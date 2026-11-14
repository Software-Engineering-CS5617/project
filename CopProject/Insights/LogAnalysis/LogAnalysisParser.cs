using System.Diagnostics;
using System.Text.Json;

namespace Insights.LogAnalysis;

/// <summary>
/// Parses and validates the structured JSON returned by Gemini log analysis.
/// </summary>
public static class LogAnalysisParser
{
    public static LogAnalysisResult Parse(string responseText)
    {
        Trace.TraceInformation("[Insights] Parsing Gemini log-analysis response.");

        if (string.IsNullOrWhiteSpace(responseText))
        {
            Trace.TraceWarning("[Insights] Gemini returned an empty log-analysis response.");
            throw new FormatException("Gemini returned an empty log-analysis response.");
        }

        string json = ExtractJsonObject(responseText);

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            string summary = ReadRequiredString(root, "summary");
            bool failureDetected = ReadRequiredBoolean(root, "failureDetected");
            string? failureLocation = ReadOptionalString(root, "failureLocation");
            string? likelyCause = ReadOptionalString(root, "likelyCause");
            IReadOnlyList<string> recommendations = ReadRecommendations(root);

            Trace.TraceInformation(
                "[Insights] Gemini log-analysis response parsed. Failure detected: {0}.",
                failureDetected);

            return new LogAnalysisResult(
                summary,
                failureDetected,
                failureLocation,
                likelyCause,
                recommendations);
        }
        catch (JsonException exception)
        {
            Trace.TraceError("[Insights] Gemini log-analysis response contained invalid JSON: {0}", exception);
            throw new FormatException("Gemini's log-analysis response was not valid JSON.", exception);
        }
        catch (FormatException exception)
        {
            Trace.TraceWarning("[Insights] Gemini log-analysis response had an invalid structure: {0}", exception.Message);
            throw;
        }
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new FormatException($"Gemini's log-analysis response must contain a non-empty '{propertyName}' string.");
        }

        return property.GetString()!;
    }

    private static bool ReadRequiredBoolean(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new FormatException($"Gemini's log-analysis response must contain a boolean '{propertyName}' value.");
        }

        return property.GetBoolean();
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"Gemini's log-analysis field '{propertyName}' must be a string or null.");
        }

        string? value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static IReadOnlyList<string> ReadRecommendations(JsonElement root)
    {
        if (!root.TryGetProperty("recommendations", out JsonElement recommendationsElement)
            || recommendationsElement.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("Gemini's log-analysis response must contain a recommendations array.");
        }

        var recommendations = new List<string>();
        foreach (JsonElement recommendation in recommendationsElement.EnumerateArray())
        {
            if (recommendation.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("Every log-analysis recommendation must be a string.");
            }

            string? text = recommendation.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                recommendations.Add(text);
            }
        }

        return Array.AsReadOnly(recommendations.ToArray());
    }

    private static string ExtractJsonObject(string responseText)
    {
        string text = responseText.Trim();
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');

        if (start < 0 || end < start)
        {
            Trace.TraceWarning("[Insights] Gemini log-analysis response did not contain a JSON object.");
            throw new FormatException("Gemini's log-analysis response did not contain a JSON object.");
        }

        return text[start..(end + 1)];
    }
}
