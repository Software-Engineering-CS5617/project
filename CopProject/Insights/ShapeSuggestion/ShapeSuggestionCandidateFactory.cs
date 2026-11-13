namespace WhiteboardSuggestion.Core;

/// <summary>
/// Creates selectable, strongly typed options from a validated AI result.
/// </summary>
public static class ShapeSuggestionCandidateFactory
{
    public static IReadOnlyList<SuggestionCandidate> Create(ShapeSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        if (!TryCreateCandidate(
            suggestion.Category,
            suggestion.BestMatch,
            suggestion.RecognizedText,
            isRecommended: true,
            out SuggestionCandidate primary))
        {
            return Array.Empty<SuggestionCandidate>();
        }

        var candidates = new List<SuggestionCandidate> { primary };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            GetKey(primary)
        };

        foreach (SuggestionCandidate alternative in suggestion.Alternatives)
        {
            if (candidates.Count == 4)
            {
                break;
            }

            if (alternative.Category != suggestion.Category
                || !TryCreateCandidate(
                    alternative.Category,
                    alternative.Shape,
                    alternative.RecognizedText,
                    isRecommended: false,
                    out SuggestionCandidate candidate))
            {
                continue;
            }

            if (seen.Add(GetKey(candidate)))
            {
                candidates.Add(candidate);
            }
        }

        return candidates;
    }

    private static bool TryCreateCandidate(
        DrawingCategory category,
        ShapeType shape,
        string? recognizedText,
        bool isRecommended,
        out SuggestionCandidate candidate)
    {
        candidate = null!;

        switch (category)
        {
            case DrawingCategory.Shape:
                if (!Enum.IsDefined(shape) || shape == ShapeType.Unknown)
                {
                    return false;
                }

                candidate = new SuggestionCandidate(category, shape, null, isRecommended);
                return true;

            case DrawingCategory.Letter:
                if (shape != ShapeType.Unknown
                    || string.IsNullOrWhiteSpace(recognizedText)
                    || recognizedText.Length != 1
                    || recognizedText[0] is < 'A' or > 'Z')
                {
                    return false;
                }

                candidate = new SuggestionCandidate(category, ShapeType.Unknown, recognizedText, isRecommended);
                return true;

            case DrawingCategory.Digit:
                if (shape != ShapeType.Unknown
                    || string.IsNullOrWhiteSpace(recognizedText)
                    || recognizedText.Length != 1
                    || recognizedText[0] is < '0' or > '9')
                {
                    return false;
                }

                candidate = new SuggestionCandidate(category, ShapeType.Unknown, recognizedText, isRecommended);
                return true;

            case DrawingCategory.Text:
                if (shape != ShapeType.Unknown || string.IsNullOrWhiteSpace(recognizedText))
                {
                    return false;
                }

                candidate = new SuggestionCandidate(
                    category,
                    ShapeType.Unknown,
                    recognizedText.Trim(),
                    isRecommended);
                return true;

            default:
                return false;
        }
    }

    private static string GetKey(SuggestionCandidate candidate)
    {
        return candidate.Category == DrawingCategory.Shape
            ? $"{candidate.Category}:{candidate.Shape}"
            : $"{candidate.Category}:{candidate.RecognizedText}";
    }
}
