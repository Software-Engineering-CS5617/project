using System;
using System.Collections.Generic;
using Whiteboard.Interfaces;
using Windows.UI;

namespace Whiteboard.Items;

public class FreehandItem : IFreehand
{
    public Guid Id { get; set; }
    public double Zindex { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public string LastModifiedBy { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Orientation { get; set; }
    public Rect2D BoundingBox { get; set; }

    public List<Point2D> Points { get; set; }
    public Color StrokeColor { get; set; }
    public double StrokeWidth { get; set; }

    public FreehandItem()
    {
        Points = new List<Point2D>();
    }

    public bool HitTest(Point2D point)
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

    private double PerpDist(Point2D p, Point2D p1, Point2D p2)
    {
        double dx = p2.X - p1.X;
        double dy = p2.Y - p1.Y;
        double p1p2Len = dx * dx + dy * dy;

        if (p1p2Len == 0)
        {
            dx = p.X - p1.X;
            dy = p.Y - p1.Y;
            return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
        }

        double t = ((p.X - p1.X) * dx + (p.Y - p1.Y) * dy) / p1p2Len;
        t = Math.Max(0, Math.Min(1, t));

        double closestX = p1.X + t * dx;
        double closestY = p1.Y + t * dy;

        return Math.Sqrt(Math.Pow(p.X - closestX, 2) + Math.Pow(p.Y - closestY, 2));
    }

}
