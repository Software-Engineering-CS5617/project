// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Whiteboard.Interfaces.Shapes;

/// <summary>
/// Represents an equilateral triangle shape
/// </summary>
public interface IEqTriangle : IShape
{
    /// <summary>
    /// Side length of the equilateral triangle
    /// </summary>
    public double SideLength { get; set; }
}
