namespace WhiteboardSuggestion.Core;

/// <summary>
/// Identifies the kind of drawing recognized in a whiteboard image.
/// </summary>
public enum DrawingCategory
{
    Unknown = 0,
    Shape,
    Letter,
    Digit,
    Text
}
