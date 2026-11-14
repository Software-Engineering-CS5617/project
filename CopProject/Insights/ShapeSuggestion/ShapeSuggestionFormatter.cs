using System.Globalization;

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Formats a suggestion as plain text, without Markdown.
/// </summary>
public static class ShapeSuggestionFormatter
{
    public static string Format(ShapeSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);
        string bestMatch = candidates.Count > 0 ? candidates[0].Label : "Unknown";
        string explanation = ShapeSuggestionParser.CleanPlainText(suggestion.Explanation);
        int confidencePercent = (int)Math.Round(
            Math.Clamp(suggestion.Confidence, 0.0, 1.0) * 100.0,
            MidpointRounding.AwayFromZero);

        List<string> lines =
        [
            $"Suggestion: {bestMatch}",
            $"Category: {suggestion.Category}"
        ];

        string[] alternatives = candidates
            .Skip(1)
            .Select(candidate => candidate.Label)
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
