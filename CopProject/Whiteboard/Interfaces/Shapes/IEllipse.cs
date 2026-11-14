/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents an ellipse shape
/// </summary>
public interface IEllipse : IShape
{
    /// <summary>
    /// Center of the ellipse
    /// </summary>
    public Point2D Center { get; }

    /// <summary>
    /// Length of the major axis of the ellipse
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Length of the minor axis of the ellipse
    /// </summary>
    public double Height { get; set; }
}
