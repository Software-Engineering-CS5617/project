/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

namespace Whiteboard.Interfaces;

/// <summary>
/// Provide live canvas animation when the mouse pointer is being dragged
/// </summary>
public interface ICanvasPreview
{
    /// <summary>
    /// Update the temporary visual item on the screen
    /// </summary>
    /// <param name="item">Item to display or update</param>
    void ShowPreview(IItem item);

    /// <summary>
    /// Clear the preview
    /// </summary>
    void ClearPreview();
}
