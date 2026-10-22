// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Rectangle : Shape, IRectangle
{
    public override Rect2D BoundingBox
    {
        get => new Rect2D {
            Left = Left,
            Top = Top,
            Width = Width,
            Height = Height
        };
        set {
            Left = value.Left;
            Top = value.Top;
            Width = value.Width;
            Height = value.Height;
        }
    }

    public override bool HitTest(Point2D point)
    {
        Rect2D box = BoundingBox;
        bool withinX = point.X >= box.Left && point.X <= box.Left + box.Width;
        bool withinY = point.Y >= box.Top && point.Y <= box.Top + box.Height;
        return withinX && withinY;
    }

    public double Width { get; set; } = 10;
    public double Height { get; set; } = 10;
}
