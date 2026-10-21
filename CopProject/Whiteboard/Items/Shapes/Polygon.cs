// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Polygon : Shape, IPolygon
{
    // TODO
    public override Rect2D BoundingBox { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    // TODO

    public override bool HitTest(Point2D point)
    {
        throw new NotImplementedException();
    }
    public List<Point2D> Vertices { get; set; } = [];
}
