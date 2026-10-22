// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents an arbitrary (closed) polygon
/// </summary>
public interface IPolygon : IShape
{

    /// <summary>
    /// List of vertices which make up the polygon
    /// </summary>
    public List<Point2D> Vertices { get; set; }
}
