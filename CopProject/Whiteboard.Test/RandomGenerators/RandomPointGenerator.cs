/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="Point2D"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomPointGenerator : BaseGenerator
{
    public static Point2D Generate()
    {
        double pointX = Rng.NextDouble() * 100;
        double pointY = Rng.NextDouble() * 100;
        Point2D point = new Point2D(pointX, pointY);
        return point;
    }
}
