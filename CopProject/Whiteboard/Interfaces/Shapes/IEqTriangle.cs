/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents an equilateral triangle shape
/// </summary>
public interface IEqTriangle : IShape
{
    /// <summary>
    /// Center of the equilateral triangle
    /// </summary>
    public Point2D Center { get; }

    /// <summary>
    /// Side length of the equilateral triangle
    /// </summary>
    public double SideLength { get; set; }
}
