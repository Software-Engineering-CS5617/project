namespace WhiteboardSuggestion.Core;

/// <summary>
/// Structured recognition result for one rough whiteboard drawing.
/// For Shape, BestMatch contains one of the supported ShapeType values and
/// RecognizedText is null. For Letter, Digit, or Text, BestMatch is Unknown
/// and RecognizedText contains the recognized content.
/// Confidence is the model's estimate, not a calibrated probability.
/// </summary>
public sealed record ShapeSuggestion(
    DrawingCategory Category,
    ShapeType BestMatch,
    string? RecognizedText,
    IReadOnlyList<SuggestionCandidate> Alternatives,
    double Confidence,
    string Explanation);
