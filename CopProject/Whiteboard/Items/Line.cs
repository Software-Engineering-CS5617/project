// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Interfaces;

namespace Whiteboard.Items;

/// <inheritdoc/>
public class Line : Item, ILine
{

    public const double DefaultHitThreshold = 5.0;
    public Point2D Begin { get; set; } = new Point2D { X = 0, Y = 0 };
    public Point2D End { get; set; } = new Point2D { X = 1, Y = 1 };

    public override Rect2D BoundingBox
    {
        get {
            double left = Math.Min(Begin.X, End.X);
            double top = Math.Min(Begin.Y, End.Y);
            double right = Math.Max(Begin.X, End.X);
            double bottom = Math.Max(Begin.Y, End.Y);

            return new Rect2D {
                Left = left,
                Top = top,
                Width = right - left,
                Height = bottom - top
            };
        }
        set {
            // Refit line to the diagonal of the new bounding box.
            Begin = new Point2D {
                X = value.Left,
                Y = value.Top
            };

            End = new Point2D {
                X = value.Left + value.Width,
                Y = value.Top + value.Height
            };
        }
    }

    public override bool HitTest(Point2D point)
    {
        double distance = PerpDist(point, Begin, End);
        return distance <= DefaultHitThreshold;
    }


}
