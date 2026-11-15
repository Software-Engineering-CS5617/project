/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generate a random placeholder text using the "Lorem Ipsum" sequence
/// </summary>
[ExcludeFromCodeCoverage]
public class LoremIpsumGenerator : BaseGenerator
{
    private static readonly string[] s_words =
    [
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit",
        "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore",
        "magna", "aliqua", "ut", "enim", "ad", "minim", "veniam", "quis", "nostrud",
        "exercitation", "ullamco", "laboris", "nisi", "ut", "aliquip", "ex", "ea",
        "commodo", "consequat", "duis", "aute", "irure", "dolor", "in", "reprehenderit",
        "in", "voluptate", "velit", "esse", "cillum", "dolore", "eu", "fugiat", "nulla",
        "pariatur", "excepteur", "sint", "occaecat", "cupidatat", "non", "proident",
        "sunt", "in", "culpa", "qui", "officia", "deserunt", "mollit", "anim", "id", "est", "laborum"
    ];

    /// <summary>
    /// Generate a random placeholder text using the "Lorem Ipsum" sequence
    /// </summary>
    /// <param name="wordCount">Number of words to generate</param>
    /// <returns>A random word sequence</returns>
    public static string Generate(int wordCount = 3)
    {
        var result = new List<string>();

        for (int i = 0; i < wordCount; i++)
        {
            result.Add(s_words[Rng.Next(s_words.Length)]);
        }

        // Capitalize the first word and add a period at the end
        if (result.Count > 0)
        {
            result[0] = char.ToUpper(result[0][0]) + result[0][1..];
            return string.Join(" ", result) + ".";
        }

        return string.Empty;
    }
}
