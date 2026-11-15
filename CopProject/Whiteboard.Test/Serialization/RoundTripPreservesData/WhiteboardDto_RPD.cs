/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Network.Dtos;
using Whiteboard.Network.Serializations;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Serialization.RoundTripPreservesData;

public class WhiteboardDto_RPD
{
    /// <summary>
    /// Test that string serialization and deserialization preserves the data
    /// </summary>
    [Fact]
    public void TestString()
    {
        for (int i = 0; i < 50; i++)
        {
            // 1. Arrange
            WhiteboardDto original = RandomWhiteboardDtoGenerator.Generate();

            // 2. Act
            string serialized = WhiteboardDtoStringSerializer.Serialize(original);
            WhiteboardDto deserialized = WhiteboardDtoStringSerializer.Deserialize(serialized);

            // 3. Assert
            Assert.Equivalent(original, deserialized);
        }
    }

    /// <summary>
    /// Test that byte serialization and deserialization preserves the data
    /// </summary>
    [Fact]
    public void TestByte()
    {
        for (int i = 0; i < 50; i++)
        {
            // 1. Arrange
            WhiteboardDto original = RandomWhiteboardDtoGenerator.Generate();

            // 2. Act
            byte[] serialized = new WhiteboardDtoByteSerializer().Serialize(original);
            WhiteboardDto deserialized = new WhiteboardDtoByteSerializer().Deserialize(serialized);

            // 3. Assert
            Assert.Equivalent(original, deserialized);
        }
    }
}
