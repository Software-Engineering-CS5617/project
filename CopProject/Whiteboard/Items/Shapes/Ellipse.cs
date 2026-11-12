// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Ellipse : Shape, IEllipse
{
    public Point2D Center { get; init; } = new(0, 0);

    public override Rect2D BoundingBox
    {
        get => new(
            Center.X - (Width / 2.0),
            Center.Y - (Height / 2.0),
            Width,
            Height
        );

        set {
            Left = value.Left + (value.Width / 2.0);
            Top = value.Top + (value.Height / 2.0);
            Width = value.Width;
            Height = value.Height;
        }
    }

    public override bool HitTest(Point2D point)
    {
        double rx = Width / 2.0;
        double ry = Height / 2.0;

        if (rx <= 0 || ry <= 0)
        {
            return false;
        }

        double dx = point.X - Center.X;
        double dy = point.Y - Center.Y;

        double normalized = ((dx * dx) / (rx * rx)) + ((dy * dy) / (ry * ry));
        return normalized <= 1.0;
    }

    public double Width { get; set; } = 10;
    public double Height { get; set; } = 20;
}
