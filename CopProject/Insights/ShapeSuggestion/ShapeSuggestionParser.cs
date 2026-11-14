using System.Text.Json;
using System.Text.RegularExpressions;

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Converts Gemini's JSON response into a validated, strongly typed result.
/// Unsupported geometric shapes are rejected instead of being passed to Whiteboard.
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

            DrawingCategory category = ParseCategory(GetString(root, "category"));
            string bestMatchText = CleanPlainText(GetString(root, "bestMatch"));
            string recognizedText = CleanPlainText(GetString(root, "recognizedText"));
            string explanation = CleanPlainText(GetString(root, "explanation"));

            if (category != DrawingCategory.Shape
                && !bestMatchText.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return CreateUnknown(
                    GetNumber(root, "confidence"),
                    "The response contained an inconsistent drawing category and bestMatch value.");
            }

            ShapeType shape = ShapeType.Unknown;
            string? normalizedRecognizedText = null;
            List<SuggestionCandidate> alternatives = [];

            switch (category)
            {
                case DrawingCategory.Shape:
                    if (!TryParseShape(bestMatchText, out shape))
                    {
                        return CreateUnknown(
                            GetNumber(root, "confidence"),
                            "The drawing does not match a supported Whiteboard shape.");
                    }

                    alternatives = ParseAlternatives(
                        GetStringArray(root, "alternatives"), category, shape);
                    break;

                case DrawingCategory.Letter:
                    if (!TryParseLetter(recognizedText, out normalizedRecognizedText))
                    {
                        return CreateUnknown(
                            GetNumber(root, "confidence"),
                            "The drawing could not be identified as a single capital letter.");
                    }

                    alternatives = ParseAlternatives(
                        GetStringArray(root, "alternatives"), category, ShapeType.Unknown,
                        normalizedRecognizedText);
                    break;

                case DrawingCategory.Digit:
                    if (!TryParseDigit(recognizedText, out normalizedRecognizedText))
                    {
                        return CreateUnknown(
                            GetNumber(root, "confidence"),
                            "The drawing could not be identified as a single digit.");
                    }

                    alternatives = ParseAlternatives(
                        GetStringArray(root, "alternatives"), category, ShapeType.Unknown,
                        normalizedRecognizedText);
                    break;

                case DrawingCategory.Text:
                    normalizedRecognizedText = NormalizeRecognizedText(recognizedText);
                    if (string.IsNullOrWhiteSpace(normalizedRecognizedText))
                    {
                        return CreateUnknown(
                            GetNumber(root, "confidence"),
                            "No readable handwritten text was identified.");
                    }

                    alternatives = ParseAlternatives(
                        GetStringArray(root, "alternatives"), category, ShapeType.Unknown,
                        normalizedRecognizedText);
                    break;

                default:
                    return CreateUnknown(GetNumber(root, "confidence"), explanation);
            }

            double confidence = ClampConfidence(GetNumber(root, "confidence"));
            return new ShapeSuggestion(
                category,
                shape,
                normalizedRecognizedText,
                alternatives,
                confidence,
                explanation);
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
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Replace("#", string.Empty, StringComparison.Ordinal);

        return Regex.Replace(result, @"\s+", " ").Trim();
    }

    private static DrawingCategory ParseCategory(string value)
    {
        return Enum.TryParse(value, ignoreCase: true, out DrawingCategory category)
            && Enum.IsDefined(category)
                ? category
                : DrawingCategory.Unknown;
    }

    private static bool TryParseShape(string value, out ShapeType shape)
    {
        string normalized = Regex.Replace(value.Trim(), @"\s+", " ");

        shape = normalized.ToLowerInvariant() switch {
            "square" => ShapeType.Square,
            "circle" => ShapeType.Circle,
            "rectangle" => ShapeType.Rectangle,
            "ellipse" or "oval" => ShapeType.Ellipse,
            "equilateral triangle" => ShapeType.EquilateralTriangle,
            "line" or "line segment" or "arrow" => ShapeType.Line,
            _ => ShapeType.Unknown
        };

        return shape != ShapeType.Unknown;
    }

    private static bool TryParseLetter(string value, out string? letter)
    {
        string normalized = RemovePrefix(value, "Letter ").Trim();
        normalized = normalized.ToUpperInvariant();

        if (normalized.Length == 1 && normalized[0] is >= 'A' and <= 'Z')
        {
            letter = normalized;
            return true;
        }

        letter = null;
        return false;
    }

    private static bool TryParseDigit(string value, out string? digit)
    {
        string normalized = RemovePrefix(value, "Digit ").Trim();

        if (normalized.Length == 1 && normalized[0] is >= '0' and <= '9')
        {
            digit = normalized;
            return true;
        }

        digit = null;
        return false;
    }

    private static string NormalizeRecognizedText(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private static List<SuggestionCandidate> ParseAlternatives(
        IEnumerable<string> source,
        DrawingCategory category,
        ShapeType primaryShape,
        string? primaryText = null)
    {
        var alternatives = new List<SuggestionCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            GetCandidateKey(category, primaryShape, primaryText)
        };

        foreach (string rawValue in source)
        {
            if (alternatives.Count == 3)
            {
                break;
            }

            string value = CleanPlainText(rawValue);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            ShapeType shape = ShapeType.Unknown;
            string? text = null;

            switch (category)
            {
                case DrawingCategory.Shape:
                    if (!TryParseShape(value, out shape))
                    {
                        continue;
                    }
                    break;

                case DrawingCategory.Letter:
                    if (!TryParseLetter(value, out text))
                    {
                        continue;
                    }
                    break;

                case DrawingCategory.Digit:
                    if (!TryParseDigit(value, out text))
                    {
                        continue;
                    }
                    break;

                case DrawingCategory.Text:
                    text = NormalizeRecognizedText(value);
                    break;

                default:
                    continue;
            }

            string key = GetCandidateKey(category, shape, text);
            if (seen.Add(key))
            {
                alternatives.Add(new SuggestionCandidate(category, shape, text, false));
            }
        }

        return alternatives;
    }

    private static string GetCandidateKey(
        DrawingCategory category,
        ShapeType shape,
        string? recognizedText)
    {
        return category == DrawingCategory.Shape
            ? $"{category}:{shape}"
            : $"{category}:{recognizedText}";
    }

    private static string RemovePrefix(string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..]
            : value;
    }

    private static ShapeSuggestion CreateUnknown(double confidence, string explanation)
    {
        return new ShapeSuggestion(
            DrawingCategory.Unknown,
            ShapeType.Unknown,
            null,
            Array.Empty<SuggestionCandidate>(),
            ClampConfidence(confidence),
            explanation);
    }

    private static double ClampConfidence(double confidence)
    {
        return double.IsFinite(confidence) ? Math.Clamp(confidence, 0.0, 1.0) : 0.0;
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
