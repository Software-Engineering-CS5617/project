// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Square : Shape, ISquare
{
    public override Rect2D BoundingBox
    {
        get => new Rect2D {
            Left = Left,
            Top = Top,
            Width = SideLegth,
            Height = SideLegth
        };
        set {
            Left = value.Left;
            Top = value.Top;
            // Keep square invariant: use smaller side
            SideLegth = value.Width < value.Height ? value.Width : value.Height;
        }
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
