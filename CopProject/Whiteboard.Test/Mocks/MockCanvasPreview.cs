/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;

namespace Whiteboard.Test.Mocks;

/// <summary>
/// Mock for canvas preview
/// </summary>
/// <inheritdoc/>
public class MockCanvasPreview : ICanvasPreview
{
    /// <summary>
    /// A history of items shown on the preview
    /// </summary>
    public List<IItem> ShownItems { get; } = [];

    /// <summary>
    /// The last itme shown on the preview
    /// </summary>
    public IItem? LastShownItem => ShownItems.Count > 0 ? ShownItems[^1] : null;

    /// <summary>
    /// Number of times ClearPreview has been called
    /// </summary>
    public int ClearCount { get; private set; }

    public void ShowPreview(IItem item)
    {
        ShownItems.Add(item);
    }

    public void ClearPreview()
    {
        ClearCount++;
    }
}
