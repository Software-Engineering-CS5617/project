/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents a rectangle shape
/// </summary>
public interface IRectangle : IShape
{
    /// <summary>
    /// Center of the rectangle
    /// </summary>
    public Point2D Center { get; }

    /// <summary>
    /// Width of the rectangle
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Height of the rectangle
    /// </summary>
    public double Height { get; set; }
}
