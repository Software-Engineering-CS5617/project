/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System.Collections.Generic;

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents an arbitrary (closed) polygon
/// </summary>
public interface IPolygon : IShape
{

    /// <summary>
    /// List of vertices which make up the polygon
    /// </summary>
    public List<Point2D> Vertices { get; set; }
}
