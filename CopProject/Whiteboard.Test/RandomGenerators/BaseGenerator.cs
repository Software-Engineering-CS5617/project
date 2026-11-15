/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Base class for other random object generators
/// </summary>
public class BaseGenerator
{
    /// <summary>
    /// Random number generator
    /// </summary>
    protected static readonly Random Rng = new();
}
