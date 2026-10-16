using System;

namespace Whiteboard.Interfaces;

public interface IItem
{
    Guid Id { get; set; }
    double Zindex { get; set; }
    string CreatedBy { get; set; }
    DateTime CreatedDateTime { get; set; }
    string LastModifiedBy { get; set; }
    DateTime LastModifiedDateTime { get; set; }
    double X { get; set; }
    double Y { get; set; }
    double Orientation { get; set; }
    Rect2D BoundingBox { get; set; }

    bool HitTest(Point2D point);
}

public struct Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
}

public struct Rect2D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
