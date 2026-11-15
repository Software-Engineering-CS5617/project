/// Owners  : Raagam Hitesh Parmar 
/// Roll No : 112301024
/// LICENSE : MIT

using System;
using Whiteboard.Interfaces;
using Windows.UI;

namespace Whiteboard.Items;

/// <inheritdoc/>
public class Line : Item, ILine
{

    public const double DefaultHitThreshold = 5.0;
    public Point2D Begin { get; set; } = new Point2D(0, 0);
    public Point2D End { get; set; } = new Point2D(1, 1);
    public Color Color { get; set; } = Windows.UI.Color.FromArgb(255, 0, 0, 0);
    public double Width { get; set; } = 3.0;

    public override Rect2D BoundingBox
    {
        get {
            double left = Math.Min(Begin.X, End.X);
            double top = Math.Min(Begin.Y, End.Y);
            double right = Math.Max(Begin.X, End.X);
            double bottom = Math.Max(Begin.Y, End.Y);

            return new Rect2D(
                left,
                top,
                right - left,
                bottom - top
            );
        }
    }

    public override void MoveBy(double dX, double dY)
    {
        Begin = Begin with { X = Begin.X + dX, Y = Begin.Y + dY };
        End = End with { X = End.X + dX, Y = End.Y + dY };
    }

    public override bool HitTest(Point2D point)
    {
        double threshold = Math.Max(DefaultHitThreshold, Width / 2.0);
        double distance = PerpDist(point, Begin, End);
        return distance <= threshold;
    }
}
