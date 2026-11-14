/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Windows.UI;

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents a closed shape on the whiteboard
/// </summary>
public interface IShape : IItem
{
    /// <summary>
    /// Fill color of the shape
    /// </summary>
    public Color FillColor { get; set; }

    /// <summary>
    /// Border color of the shape
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Border width of the shape
    /// </summary>
    public double BorderWidth { get; set; }
}
