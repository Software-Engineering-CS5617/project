/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="ItemMetadata"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomItemMetadataGenerator : BaseGenerator
{
    /// <summary>
    /// Allowed characters in the username
    /// </summary>
    private static readonly string s_allowed_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";

    public static ItemMetadata Generate()
    {
        // 1. Generate a GUID
        Guid id = Guid.NewGuid();

        // 2. Generate a random z-index
        double zIndex = Rng.NextDouble() * 100;

        // 3. Generate a random string for creator
        int stringLength = Rng.Next(34);
        string createdBy = Rng.GetString(s_allowed_chars, stringLength);


        // 4. get current time
        DateTime currentTime = DateTime.Now;
        DateTime createdDateTime = currentTime;
        DateTime modifiedDateTime = currentTime;

        // 5. Generate a rando orientation
        double orientation = Rng.NextDouble() * 360;

        // 6. Generate item metadata
        ItemMetadata metaData = new(id, zIndex, createdBy, createdDateTime, modifiedDateTime, orientation);
        return metaData;
    }
}
