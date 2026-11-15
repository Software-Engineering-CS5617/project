/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;

namespace Whiteboard.Test.RandomGenerators;

[ExcludeFromCodeCoverage]
public class RandomPointListGenerator : BaseGenerator
{
    /// <summary>
    /// Generates a random list of <see cref="Point2D"/>
    /// </summary>
    /// <param name="min">Minimum number of points (default 2)</param>
    /// <param name="max">Maximum number of points (default 50)</param>
    /// <returns></returns>
    public static List<Point2D> Generate(int min = 2, int max = 50)
    {
        int nPoints = Rng.Next(min, max);
        List<Point2D> points = [];

        for (int i = 0; i < nPoints; i++)
        {
            Point2D point = RandomPointGenerator.Generate();

            points.Add(point);
        }

        return points;
    }
}
