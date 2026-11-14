/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Rectangle : Shape, IRectangle
{
    public Point2D Center { get; set; } = new(0, 0);

    public override Rect2D BoundingBox => new(
        Center.X - (Width / 2.0),
        Center.Y - (Height / 2.0),
        Width,
        Height
    );

    public override void MoveBy(double dX, double dY)
    {
        Center = Center with { X = Center.X + dX, Y = Center.Y + dY };
    }

    public override bool HitTest(Point2D point)
    {
        Rect2D box = BoundingBox;
        bool withinX = point.X >= box.Left && point.X <= box.Left + box.Width;
        bool withinY = point.Y >= box.Top && point.Y <= box.Top + box.Height;
        return withinX && withinY;
    }

    public double Width { get; set; } = 10;
    public double Height { get; set; } = 10;
}
