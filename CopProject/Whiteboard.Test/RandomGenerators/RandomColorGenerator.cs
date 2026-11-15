/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="Color"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomColorGenerator : BaseGenerator
{
    /// <summary>
    /// Random colors for testing
    /// </summary>
    private static readonly string[] s_randomHexColors =
        [
            "#FFFF0000",    // Red
            "#FF000000",    // Black
            "#FF0000FF",    // Blue
            "#FFFFFF00",    // Yellow
            "#FF008000"     // Green
        ];

    public static string Generate()
    {
        int randomIndex = Rng.Next(s_randomHexColors.Length);
        string randomColor = s_randomHexColors[randomIndex];

        return randomColor;
    }
}
