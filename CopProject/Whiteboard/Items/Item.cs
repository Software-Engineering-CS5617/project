// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Interfaces;

namespace Whiteboard.Items;

/// <inheritdoc/>
public abstract class Item : IItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public double Zindex { get; set; } = 0;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTime LastModifiedDateTime { get; set; } = DateTime.Now;
    public double Left { get; set; } = 0;
    public double Top { get; set; } = 0;
    public double Orientation { get; set; } = 0;
    public abstract Rect2D BoundingBox { get; set; }
    public abstract bool HitTest(Point2D point);

    /// <summary>
    /// Calculates the shortest (perpendicular) distance of a point p
    /// to a line segment defined by endpoints p1 and p2.
    /// </summary>
    /// <param name="p">Point to check perpendicular distance for</param>
    /// <param name="p1">First endpoint of the line</param>
    /// <param name="p2">Second endpoint of the line</param>
    /// <returns>Euclidean distance between p and the closest point</returns>
    protected static double PerpDist(Point2D p, Point2D p1, Point2D p2)
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
