// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class EqTriangle : Shape, IEqTriangle
{
    public override Rect2D BoundingBox
    {
        get => new Rect2D {
            Left = Left,
            Top = Top,
            Width = SideLength,
            Height = GetTriangleHeight(SideLength)
        };
        set {
            Left = value.Left;
            Top = value.Top;

            double sideFromWidth = value.Width;
            double sideFromHeight = value.Height * 2.0 / Math.Sqrt(3.0);
            SideLength = sideFromWidth < sideFromHeight ? sideFromWidth : sideFromHeight;
        }
    }

    public override bool HitTest(Point2D point)
    {
        Point2D p1 = new Point2D { X = Left + (SideLength / 2.0), Y = Top };
        Point2D p2 = new Point2D { X = Left, Y = Top + GetTriangleHeight(SideLength) };
        Point2D p3 = new Point2D { X = Left + SideLength, Y = Top + GetTriangleHeight(SideLength) };

        bool b1 = Sign(point, p1, p2) < 0.0;
        bool b2 = Sign(point, p2, p3) < 0.0;
        bool b3 = Sign(point, p3, p1) < 0.0;

        return (b1 == b2) && (b2 == b3);
    }
    private static double GetTriangleHeight(double sideLength)
    {
        return (Math.Sqrt(3.0) / 2.0) * sideLength;
    }

    private static double Sign(Point2D p1, Point2D p2, Point2D p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }

    public double SideLength { get; set; } = 10;
}
