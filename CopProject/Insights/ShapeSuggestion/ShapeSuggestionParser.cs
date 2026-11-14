using System.Diagnostics;
using System.Text.Json;

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Converts Gemini's structured response into a validated geometric result.
/// </summary>
public static class ShapeSuggestionParser
{
    public static ShapeSuggestionResult Parse(string responseText)
    {
        Trace.TraceInformation("[Insights] Parsing Gemini shape recognition response.");

        if (string.IsNullOrWhiteSpace(responseText))
        {
            Trace.TraceWarning("[Insights] Gemini returned an empty shape recognition response.");
            throw new FormatException("Gemini returned an empty response.");
        }

        string json = ExtractJsonObject(responseText);

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            // ExtractJsonObject returns the substring beginning with '{' and ending with '}',
            // so the parsed root is an object whenever JSON parsing succeeds.
            JsonElement root = document.RootElement;

            double confidence = ReadConfidence(root);
            string resultType = GetString(root, "type");

            ShapeSuggestionResult result = resultType.Trim().ToLowerInvariant() switch
            {
                "unknown" => new ShapeSuggestionResult.Unknown(confidence),
                "line" => ParseLine(root, confidence),
                "polygon" => ParsePolygon(root, confidence),
                "circle" => ParseCircle(root, confidence),
                "ellipse" => ParseEllipse(root, confidence),
                _ => InvalidGeometry(confidence, "unsupported or missing result type")
            };

            Trace.TraceInformation(
                "[Insights] Gemini shape response parsed as '{0}'.",
                result.GetType().Name);

            return result;
        }
        catch (JsonException exception)
        {
            Trace.TraceError("[Insights] Gemini shape response contained invalid JSON: {0}", exception);
            throw new FormatException("Gemini's response was not valid JSON.", exception);
        }
    }

    private static ShapeSuggestionResult ParseLine(JsonElement root, double confidence)
    {
        if (!TryGetFiniteNumber(root, "startX", out double startX)
            || !TryGetFiniteNumber(root, "startY", out double startY)
            || !TryGetFiniteNumber(root, "endX", out double endX)
            || !TryGetFiniteNumber(root, "endY", out double endY)
            || (startX == endX && startY == endY))
        {
            return InvalidGeometry(confidence, "line endpoints were missing, non-finite, or identical");
        }

        return new ShapeSuggestionResult.Line(
            (startX, startY),
            (endX, endY),
            confidence);
    }

    private static ShapeSuggestionResult ParsePolygon(JsonElement root, double confidence)
    {
        if (!root.TryGetProperty("vertices", out JsonElement verticesElement)
            || verticesElement.ValueKind != JsonValueKind.Array)
        {
            return InvalidGeometry(confidence, "polygon vertices were missing or not an array");
        }

        var vertices = new List<(double X, double Y)>();
        foreach (JsonElement vertex in verticesElement.EnumerateArray())
        {
            if (vertex.ValueKind != JsonValueKind.Object
                || !TryGetFiniteNumber(vertex, "x", out double x)
                || !TryGetFiniteNumber(vertex, "y", out double y))
            {
                return InvalidGeometry(confidence, "polygon contained an invalid vertex");
            }

            var point = (X: x, Y: y);
            if (vertices.Count == 0 || vertices[^1] != point)
            {
                vertices.Add(point);
            }
        }

        // The public contract stores each corner once; remove a repeated closing point.
        if (vertices.Count > 1 && vertices[0] == vertices[^1])
        {
            vertices.RemoveAt(vertices.Count - 1);
        }

        if (vertices.Distinct().Count() < 3)
        {
            return InvalidGeometry(confidence, "polygon contained fewer than three distinct vertices");
        }

        return new ShapeSuggestionResult.Polygon(
            Array.AsReadOnly(vertices.ToArray()),
            confidence);
    }

    private static ShapeSuggestionResult ParseCircle(JsonElement root, double confidence)
    {
        if (!TryGetFiniteNumber(root, "centerX", out double centerX)
            || !TryGetFiniteNumber(root, "centerY", out double centerY)
            || !TryGetFiniteNumber(root, "radius", out double radius)
            || radius <= 0)
        {
            return InvalidGeometry(confidence, "circle centre or positive radius was invalid");
        }

        return new ShapeSuggestionResult.Circle(
            (centerX, centerY),
            radius,
            confidence);
    }

    private static ShapeSuggestionResult ParseEllipse(JsonElement root, double confidence)
    {
        if (!TryGetFiniteNumber(root, "centerX", out double centerX)
            || !TryGetFiniteNumber(root, "centerY", out double centerY)
            || !TryGetFiniteNumber(root, "radiusX", out double radiusX)
            || !TryGetFiniteNumber(root, "radiusY", out double radiusY)
            || radiusX <= 0
            || radiusY <= 0)
        {
            return InvalidGeometry(confidence, "ellipse centre or positive radii were invalid");
        }

        return new ShapeSuggestionResult.Ellipse(
            (centerX, centerY),
            radiusX,
            radiusY,
            confidence);
    }

    private static ShapeSuggestionResult.Unknown InvalidGeometry(
        double confidence,
        string reason)
    {
        Trace.TraceWarning("[Insights] Shape response could not be represented as valid geometry: {0}.", reason);
        return new ShapeSuggestionResult.Unknown(confidence);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : string.Empty;
    }

    private static double ReadConfidence(JsonElement root)
    {
        if (TryGetFiniteNumber(root, "confidence", out double confidence))
        {
            return Math.Clamp(confidence, 0.0, 1.0);
        }

        Trace.TraceWarning(
            "[Insights] Shape response did not contain a valid confidence value; defaulting to 0.");
        return 0.0;
    }

    private static bool TryGetFiniteNumber(
        JsonElement element,
        string propertyName,
        out double value)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out value)
            && double.IsFinite(value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static string ExtractJsonObject(string responseText)
    {
        string text = responseText.Trim();
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');

        if (start < 0 || end < start)
        {
            Trace.TraceWarning("[Insights] Gemini shape response did not contain a JSON object.");
            throw new FormatException("Gemini's response did not contain a JSON object.");
        }

        return text[start..(end + 1)];
    }
}
