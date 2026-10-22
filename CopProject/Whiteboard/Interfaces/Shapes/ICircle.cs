// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents a circle shape
/// </summary>
public interface ICircle : IShape
{
    /// <summary>
    /// Radius of the circle
    /// </summary>
    public double Radius { get; set; }
}
