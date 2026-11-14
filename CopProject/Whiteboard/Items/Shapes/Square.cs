/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Square : Shape, ISquare
{
    public Point2D Center { get; set; } = new(0, 0);

    public override Rect2D BoundingBox => new(
        Center.X - (SideLegth / 2.0),
        Center.Y - (SideLegth / 2.0),
        SideLegth,
        SideLegth
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

    public double SideLegth { get; set; } = 10;
}
