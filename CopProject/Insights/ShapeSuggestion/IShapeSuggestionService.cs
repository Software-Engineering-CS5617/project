namespace WhiteboardSuggestion.Core;

/// <summary>
/// Recognizes a stroke trajectory and returns its inferred geometric representation.
/// </summary>
public interface IShapeSuggestionService
{
    Task<ShapeSuggestionResult> SuggestShapeAsync(
        StrokeRecognitionRequest request);
}
