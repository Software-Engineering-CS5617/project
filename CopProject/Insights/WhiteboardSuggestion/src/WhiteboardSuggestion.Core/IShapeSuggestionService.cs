namespace WhiteboardSuggestion.Core;

/// <summary>
/// Reusable interface for a UI or another module to request a drawing suggestion.
/// Pass the bytes of a cropped image containing one drawing, plus its MIME type.
/// </summary>
public interface IShapeSuggestionService
{
    Task<ShapeSuggestion> SuggestAsync(byte[] imageBytes, string mimeType);
}
