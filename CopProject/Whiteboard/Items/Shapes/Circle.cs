/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Circle : Shape, ICircle
{
    public Point2D Center { get; set; } = new(0, 0);

    public double Radius { get; set; } = 10;

    public override Rect2D BoundingBox => new(
        Center.X - Radius,
        Center.Y - Radius,
        Radius * 2,
        Radius * 2
    );

    public override void MoveBy(double dX, double dY)
    {
        Center = Center with { X = Center.X + dX, Y = Center.Y + dY };
    }

    public override bool HitTest(Point2D point)
    {
        double dx = point.X - Center.X;
        double dy = point.Y - Center.Y;
        double distanceSquared = (dx * dx) + (dy * dy);
        double radiusSquared = Radius * Radius;
        return distanceSquared <= radiusSquared;
    }
}
