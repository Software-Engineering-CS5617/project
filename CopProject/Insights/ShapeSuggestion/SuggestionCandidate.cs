namespace WhiteboardSuggestion.Core;

/// <summary>
/// A selectable suggestion. Shape is used for geometric drawings;
/// RecognizedText is used for letters, digits, and handwritten text.
/// </summary>
public sealed record SuggestionCandidate(
    DrawingCategory Category,
    ShapeType Shape,
    string? RecognizedText,
    bool IsRecommended)
{
    /// <summary>
    /// A UI-friendly label for displaying this candidate.
    /// </summary>
    public string Label => Category switch {
        DrawingCategory.Shape => Shape switch {
            ShapeType.EquilateralTriangle => "Equilateral Triangle",
            ShapeType.Unknown => "Unknown",
            _ => Shape.ToString()
        },
        DrawingCategory.Letter => $"Letter {RecognizedText}",
        DrawingCategory.Digit => $"Digit {RecognizedText}",
        DrawingCategory.Text => RecognizedText ?? string.Empty,
        _ => "Unknown"
    };
}
