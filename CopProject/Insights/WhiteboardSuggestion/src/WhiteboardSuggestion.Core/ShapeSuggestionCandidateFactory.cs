namespace WhiteboardSuggestion.Core;

/// <summary>
/// Converts the AI result into simple clickable options for any UI framework.
/// Keeping this outside WPF lets a WinUI or other whiteboard host reuse the same logic.
/// </summary>
public static class ShapeSuggestionCandidateFactory
{
    public static IReadOnlyList<SuggestionCandidate> Create(ShapeSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        var candidates = new List<SuggestionCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Add(suggestion.BestMatch, suggestion.Category, isRecommended: true);

        foreach (string alternative in suggestion.Alternatives.Take(3))
        {
            Add(alternative, suggestion.Category, isRecommended: false);
        }

        return candidates;

        void Add(string? sourceLabel, DrawingCategory fallbackCategory, bool isRecommended)
        {
            if (string.IsNullOrWhiteSpace(sourceLabel))
            {
                return;
            }

            string raw = sourceLabel.Trim();
            DrawingCategory category = InferCategory(raw, fallbackCategory);

            if (category == DrawingCategory.Unknown
                && raw.Equals("Unclear", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string value = GetCanonicalValue(raw, category);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string key = $"{category}:{value}";
            if (!seen.Add(key))
            {
                return;
            }

            string label = GetDisplayLabel(raw, category, value);
            candidates.Add(new SuggestionCandidate(label, category, value, isRecommended));
        }
    }

    private static DrawingCategory InferCategory(string label, DrawingCategory fallback)
    {
        if (label.StartsWith("Letter ", StringComparison.OrdinalIgnoreCase))
        {
            return DrawingCategory.Letter;
        }

        if (label.StartsWith("Digit ", StringComparison.OrdinalIgnoreCase))
        {
            return DrawingCategory.Digit;
        }

        if (fallback == DrawingCategory.Letter && label.Length == 1 && char.IsLetter(label[0]))
        {
            return DrawingCategory.Letter;
        }

        if (fallback == DrawingCategory.Digit && label.Length == 1 && char.IsDigit(label[0]))
        {
            return DrawingCategory.Digit;
        }

        return fallback;
    }

    private static string GetCanonicalValue(string label, DrawingCategory category)
    {
        return category switch
        {
            DrawingCategory.Letter => RemovePrefix(label, "Letter ").Trim().ToUpperInvariant(),
            DrawingCategory.Digit => RemovePrefix(label, "Digit ").Trim(),
            _ => label
        };
    }

    private static string GetDisplayLabel(string raw, DrawingCategory category, string value)
    {
        return category switch
        {
            DrawingCategory.Letter => $"Letter {value}",
            DrawingCategory.Digit => $"Digit {value}",
            _ => raw
        };
    }

    private static string RemovePrefix(string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..]
            : value;
    }
}
