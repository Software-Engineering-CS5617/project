using System.Text.Json;
using System.Text.RegularExpressions;

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Converts Gemini's JSON response into a safe, normalized domain object.
/// Also tolerates Markdown code fences as a defensive fallback.
/// </summary>
public static class ShapeSuggestionParser
{
    public static ShapeSuggestion Parse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new FormatException("Gemini returned an empty response.");
        }

        string json = ExtractJsonObject(responseText);

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("Gemini's response must be a JSON object.");
            }

            string categoryText = GetString(root, "category");
            DrawingCategory category = Enum.TryParse(
                CleanPlainText(categoryText),
                ignoreCase: true,
                out DrawingCategory parsedCategory)
                ? parsedCategory
                : DrawingCategory.Unknown;

            string bestMatch = CleanPlainText(GetString(root, "bestMatch"));
            if (string.IsNullOrWhiteSpace(bestMatch))
            {
                bestMatch = "Unclear";
                category = DrawingCategory.Unknown;
            }

            List<string> alternatives = GetStringArray(root, "alternatives")
                .Select(CleanPlainText)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Where(value => !value.Equals(bestMatch, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            double confidence = GetNumber(root, "confidence");
            confidence = double.IsFinite(confidence) ? Math.Clamp(confidence, 0.0, 1.0) : 0.0;

            string explanation = CleanPlainText(GetString(root, "explanation"));

            return new ShapeSuggestion(category, bestMatch, alternatives, confidence, explanation);
        }
        catch (JsonException exception)
        {
            throw new FormatException("Gemini's response was not valid JSON.", exception);
        }
    }

    /// <summary>
    /// Removes common Markdown markers so only readable text reaches the UI.
    /// Structured JSON remains the preferred response format; this is a fallback.
    /// </summary>
    public static string CleanPlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string result = value.Trim();
        result = Regex.Replace(result, @"^\s{0,3}#{1,6}\s*", string.Empty, RegexOptions.Multiline);
        result = Regex.Replace(result, @"^\s*[-+]\s+", string.Empty, RegexOptions.Multiline);
        result = result.Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal)
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Replace("#", string.Empty, StringComparison.Ordinal);

        return Regex.Replace(result, @"\s+", " ").Trim();
    }

    private static string ExtractJsonObject(string responseText)
    {
        string trimmed = responseText.Trim();
        int firstBrace = trimmed.IndexOf('{');
        int lastBrace = trimmed.LastIndexOf('}');

        if (firstBrace < 0 || lastBrace < firstBrace)
        {
            throw new FormatException("Gemini's response did not contain a JSON object.");
        }

        return trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static IEnumerable<string> GetStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty);
    }

    private static double GetNumber(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out double number))
        {
            return number;
        }

        return 0.0;
    }
}
