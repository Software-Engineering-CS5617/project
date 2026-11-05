using System.Globalization;

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Formats a suggestion as plain console/UI text, without Markdown.
/// </summary>
public static class ShapeSuggestionFormatter
{
    public static string Format(ShapeSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        string bestMatch = ShapeSuggestionParser.CleanPlainText(suggestion.BestMatch);
        string explanation = ShapeSuggestionParser.CleanPlainText(suggestion.Explanation);
        string category = suggestion.Category.ToString();
        int confidencePercent = (int)Math.Round(
            Math.Clamp(suggestion.Confidence, 0.0, 1.0) * 100.0,
            MidpointRounding.AwayFromZero);

        List<string> lines =
        [
            $"Suggestion: {(string.IsNullOrWhiteSpace(bestMatch) ? "Unclear" : bestMatch)}",
            $"Category: {category}"
        ];

        string[] alternatives = suggestion.Alternatives
            .Select(ShapeSuggestionParser.CleanPlainText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Where(value => !value.Equals(bestMatch, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        if (alternatives.Length > 0)
        {
            lines.Add($"Other possibilities: {string.Join(", ", alternatives)}");
        }

        lines.Add($"Confidence: {confidencePercent.ToString(CultureInfo.InvariantCulture)}%");

        if (!string.IsNullOrWhiteSpace(explanation))
        {
            lines.Add($"Reason: {explanation}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
