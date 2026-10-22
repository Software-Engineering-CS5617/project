// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Whiteboard.Interfaces.Shapes;


/// <summary>
/// Represents a square shape
/// </summary>
public interface ISquare : IShape
{
    /// <summary>
    /// Side length of the square
    /// </summary>
    public double SideLegth { get; set; }
}
