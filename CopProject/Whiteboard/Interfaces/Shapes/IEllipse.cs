// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents an ellipse shape
/// </summary>
public interface IEllipse : IShape
{
    /// <summary>
    /// Length of the major axis of the ellipse
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Length of the minor axis of the ellipse
    /// </summary>
    public double Height { get; set; }
}
