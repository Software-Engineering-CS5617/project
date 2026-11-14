/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Collections.Generic;
using System.Linq;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Polygon : Shape, IPolygon
{
    public override Rect2D BoundingBox
    {
        get {
            if (Vertices == null || Vertices.Count == 0)
            {
                return new Rect2D(0, 0, 0, 0);
            }

            double minX = Vertices.Min(v => v.X);
            double maxX = Vertices.Max(v => v.X);
            double minY = Vertices.Min(v => v.Y);
            double maxY = Vertices.Max(v => v.Y);

            return new Rect2D(
                minX,
                minY,
                maxX - minX,
                maxY - minY
            );
        }
    }

    public override void MoveBy(double dX, double dY)
    {
        for (int i = 0; i < Vertices.Count; i++)
        {
            Vertices[i] = Vertices[i] with { X = Vertices[i].X + dX, Y = Vertices[i].Y + dY };
        }
    }

    public override bool HitTest(Point2D point)
    {
        throw new NotImplementedException();
    }

    public List<Point2D> Vertices { get; set; } = [];
}
