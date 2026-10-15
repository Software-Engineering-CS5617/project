using System.Collections.Generic;
using Windows.UI;

namespace Whiteboard.Interfaces
{
    public interface IFreehand : IItem
    {
        List<Point2D> Points { get; set; }
        Color StrokeColor { get; set; }
        double StrokeWidth { get; set; }
    }
}
