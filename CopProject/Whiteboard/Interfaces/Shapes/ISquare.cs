/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents a square shape
/// </summary>
public interface ISquare : IShape
{
    /// <summary>
    /// Center of the square
    /// </summary>
    public Point2D Center { get; }

    /// <summary>
    /// Side length of the square
    /// </summary>
    public double SideLegth { get; set; }
}
