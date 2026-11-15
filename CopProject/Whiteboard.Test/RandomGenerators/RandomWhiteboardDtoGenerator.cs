/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Test.RandomGenerators;

[ExcludeFromCodeCoverage]
public class RandomWhiteboardDtoGenerator : BaseGenerator
{
    /// <summary>
    /// Generates a random <see cref="WhiteboardDto"/> containing <paramref name="count"/> elements
    /// </summary>
    /// <param name="count">Number of elements to generate</param>
    /// <returns>Random <see cref="WhiteboardDto"/></returns>
    public static WhiteboardDto Generate(int count = 5)
    {
        var items = new List<ItemDto>();
        for (int i = 0; i < count; i++)
        {
            items.Add(RandomItemDtoGenerator.Generate());
        }

        return new WhiteboardDto(items);
    }
}
