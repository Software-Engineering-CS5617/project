namespace WhiteboardSuggestion.Core;

/// <summary>
/// A clickable candidate shown by the whiteboard suggestion overlay.
/// CanonicalValue is the value the board should draw, for example "M", "8", or "Circle".
/// </summary>
public sealed record SuggestionCandidate(
    string Label,
    DrawingCategory Category,
    string CanonicalValue,
    bool IsRecommended);
