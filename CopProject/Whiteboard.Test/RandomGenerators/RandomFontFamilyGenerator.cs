/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random font family
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomFontFamilyGenerator : BaseGenerator
{
    /// <summary>
    /// Random font families for testing
    /// </summary>
    private static readonly List<string> s_random_font_families = ["Segoe UI", "Arial", "Consolas", "Calibri"];
    public static string Generate()
    {
        int randomIndex = Rng.Next(s_random_font_families.Count);
        string randomFamily = s_random_font_families[randomIndex];

        return randomFamily;
    }
}
