namespace WhiteboardSuggestion.Core;

/// <summary>
/// Reusable interface for a UI or another module to request recognition of one drawing.
/// Pass the bytes of a cropped image containing one drawing, plus its MIME type.
/// Shape results use ShapeType; letter, digit, and text results use RecognizedText.
/// </summary>
public interface IShapeSuggestionService
{
    Task<ShapeSuggestion> SuggestAsync(byte[] imageBytes, string mimeType);
}
