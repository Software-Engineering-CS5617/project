using System.Collections.Generic;
using Windows.UI;

namespace Whiteboard.Interfaces;

/// <summary>
/// Represents a freehand (stroke) item
/// </summary>
public interface IFreehand : IItem
{
    /// <summary>
    /// List of points which make up the freehand item
    /// </summary>
    List<Point2D> Points { get; set; }

    /// <summary>
    /// Color of the freehand item
    /// </summary>
    Color StrokeColor { get; set; }

    /// <summary>
    /// Width of the stroke item
    /// </summary>
    double StrokeWidth { get; set; }
}
