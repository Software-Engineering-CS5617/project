/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents a circle shape
/// </summary>
public interface ICircle : IShape
{
    /// <summary>
    /// Center of the circle
    /// </summary>
    public Point2D Center { get; }

    /// <summary>
    /// Radius of the circle
    /// </summary>
    public double Radius { get; set; }
}
