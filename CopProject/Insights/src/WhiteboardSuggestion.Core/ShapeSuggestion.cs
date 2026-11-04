namespace WhiteboardSuggestion.Core;

/// <summary>
/// A canonical suggestion for one rough whiteboard drawing.
/// Confidence is the model's estimate, not a calibrated probability.
/// </summary>
public sealed record ShapeSuggestion(
    DrawingCategory Category,
    string BestMatch,
    IReadOnlyList<string> Alternatives,
    double Confidence,
    string Explanation);
