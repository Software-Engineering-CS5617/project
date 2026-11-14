using System.Text.Json;

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Converts Gemini's structured response into a validated geometric result.
/// </summary>
public static class ShapeSuggestionParser
{
    public static ShapeSuggestionResult Parse(string responseText)
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

            double confidence = ReadConfidence(root);
            string resultType = GetString(root, "type");

            return resultType.Trim().ToLowerInvariant() switch
            {
                "unknown" => new ShapeSuggestionResult.Unknown(confidence),
                "line" => ParseLine(root, confidence),
                "polygon" => ParsePolygon(root, confidence),
                "circle" => ParseCircle(root, confidence),
                "ellipse" => ParseEllipse(root, confidence),
                _ => new ShapeSuggestionResult.Unknown(confidence)
            };
        }
        catch (JsonException exception)
        {
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
            return new ShapeSuggestionResult.Unknown(confidence);
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
            return new ShapeSuggestionResult.Unknown(confidence);
        }

        var vertices = new List<(double X, double Y)>();
        foreach (JsonElement vertex in verticesElement.EnumerateArray())
        {
            if (vertex.ValueKind != JsonValueKind.Object
                || !TryGetFiniteNumber(vertex, "x", out double x)
                || !TryGetFiniteNumber(vertex, "y", out double y))
            {
                return new ShapeSuggestionResult.Unknown(confidence);
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
            return new ShapeSuggestionResult.Unknown(confidence);
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
            return new ShapeSuggestionResult.Unknown(confidence);
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
            return new ShapeSuggestionResult.Unknown(confidence);
        }

        return new ShapeSuggestionResult.Ellipse(
            (centerX, centerY),
            radiusX,
            radiusY,
            confidence);
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
        return TryGetFiniteNumber(root, "confidence", out double confidence)
            ? Math.Clamp(confidence, 0.0, 1.0)
            : 0.0;
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
            throw new FormatException("Gemini's response did not contain a JSON object.");
        }

        return text[start..(end + 1)];
    }
}
