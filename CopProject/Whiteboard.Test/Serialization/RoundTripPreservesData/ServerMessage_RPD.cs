/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Serialization.RoundTripPreservesData;

/// <summary>
/// An enum representing differen test cases for xUnit
/// </summary>
public enum XUnitServerMessageType
{
    ResUpdate,
    ResAddItem,
    ResDeleteItem,
    ResUpdateItem
}

public class ServerMessage_RPD
{
    /// <summary>
    /// Test that string serialization and deserialization preserves the data
    /// </summary>
    /// <param name="type">One of <see cref="XUnitServerMessageType"/></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Theory]
    [InlineData(XUnitServerMessageType.ResUpdate)]
    [InlineData(XUnitServerMessageType.ResAddItem)]
    [InlineData(XUnitServerMessageType.ResDeleteItem)]
    [InlineData(XUnitServerMessageType.ResUpdateItem)]
    public void TestString(XUnitServerMessageType type)
    {
        for (int i = 0; i < 50; i++)
        {
            ServerMessage original = type switch {
                XUnitServerMessageType.ResUpdate =>
                    new ServerMessage.ResUpdate(RandomWhiteboardDtoGenerator.Generate()),

                XUnitServerMessageType.ResAddItem =>
                    new ServerMessage.ResAddItem(Guid.NewGuid(), RandomItemDtoGenerator.Generate()),

                XUnitServerMessageType.ResDeleteItem =>
                    new ServerMessage.ResDeleteItem(Guid.NewGuid()),

                XUnitServerMessageType.ResUpdateItem =>
                    new ServerMessage.ResUpdateItem(Guid.NewGuid(), RandomItemDtoGenerator.Generate()),

                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            string serialized = ServerMessageStringSerializer.Serialize(original);
            ServerMessage deserialized = ServerMessageStringSerializer.Deserialize(serialized);

            Assert.Equivalent(original, deserialized);
        }
    }

    /// <summary>
    /// Test that byte serialization and deserialization preserves the data
    /// </summary>
    /// <param name="type">One of <see cref="XUnitServerMessageType"/></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Theory]
    [InlineData(XUnitServerMessageType.ResUpdate)]
    [InlineData(XUnitServerMessageType.ResAddItem)]
    [InlineData(XUnitServerMessageType.ResDeleteItem)]
    [InlineData(XUnitServerMessageType.ResUpdateItem)]
    public void TestByte(XUnitServerMessageType type)
    {
        for (int i = 0; i < 50; i++)
        {
            ServerMessage original = type switch {
                XUnitServerMessageType.ResUpdate =>
                    new ServerMessage.ResUpdate(RandomWhiteboardDtoGenerator.Generate()),

                XUnitServerMessageType.ResAddItem =>
                    new ServerMessage.ResAddItem(Guid.NewGuid(), RandomItemDtoGenerator.Generate()),

                XUnitServerMessageType.ResDeleteItem =>
                    new ServerMessage.ResDeleteItem(Guid.NewGuid()),

                XUnitServerMessageType.ResUpdateItem =>
                    new ServerMessage.ResUpdateItem(Guid.NewGuid(), RandomItemDtoGenerator.Generate()),

                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            byte[] serialized = new ServerMessageByteSerializer().Serialize(original);
            ServerMessage deserialized = new ServerMessageByteSerializer().Deserialize(serialized);

            Assert.Equivalent(original, deserialized);
        }
    }
}
