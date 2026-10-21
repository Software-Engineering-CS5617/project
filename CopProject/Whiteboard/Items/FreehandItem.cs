using System;
using System.Collections.Generic;
using Whiteboard.Interfaces;
using Windows.UI;

namespace Whiteboard.Items;

public class FreehandItem : Item, IFreehand
{
    public override Rect2D BoundingBox { get; set; }
    public List<Point2D> Points { get; set; }
    public Color StrokeColor { get; set; }
    public double StrokeWidth { get; set; }

    public FreehandItem()
    {
        Points = new List<Point2D>();
    }

    public override bool HitTest(Point2D point)
    {
        if (Points == null || Points.Count < 2)
        {
            return false;
        }
        double clickThreshold = (StrokeWidth / 2) + 5;

        for (int i = 0; i < Points.Count - 1; i++)
        {
            Point2D p1 = Points[i];
            Point2D p2 = Points[i + 1];
            double dist = PerpDist(point, p1, p2);

            if (dist <= clickThreshold)
            {
                return true;
            }
        }
        return false;
    }
}
