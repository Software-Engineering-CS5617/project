// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Interfaces;

namespace Whiteboard.Items;

/// <inheritdoc/>
public class Line : Item, ILine
{
    // TODO
    public override Rect2D BoundingBox { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    // TODO
    public override bool HitTest(Point2D point)
    {
        throw new NotImplementedException();
    }

    public Point2D Begin { get; set; } = new Point2D { X = 0, Y = 0 };
    public Point2D End {  get; set; } = new Point2D { X = 1, Y = 1 };
}
