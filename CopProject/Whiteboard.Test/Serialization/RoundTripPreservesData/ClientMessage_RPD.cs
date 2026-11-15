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
public enum XUnitClientMessageType
{
    ReqUpdate,
    ReqAddItem,
    ReqDeleteItem,
    ReqUpdateItem
}

public class ClientMessage_RPD
{
    /// <summary>
    /// Test that string serialization and deserialization preserves the data
    /// </summary>
    /// <param name="type">One of <see cref="XUnitClientMessageType"/></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Theory]
    [InlineData(XUnitClientMessageType.ReqUpdate)]
    [InlineData(XUnitClientMessageType.ReqAddItem)]
    [InlineData(XUnitClientMessageType.ReqDeleteItem)]
    [InlineData(XUnitClientMessageType.ReqUpdateItem)]
    public void TestString(XUnitClientMessageType type)
    {
        for (int i = 0; i < 50; i++)
        {
            ClientMessage original = type switch {
                XUnitClientMessageType.ReqUpdate =>
                    new ClientMessage.ReqUpdate(),

                XUnitClientMessageType.ReqAddItem =>
                    new ClientMessage.ReqAddItem(Guid.NewGuid(), RandomItemDtoGenerator.Generate()),

                XUnitClientMessageType.ReqDeleteItem =>
                    new ClientMessage.ReqDeleteItem(Guid.NewGuid()),

                XUnitClientMessageType.ReqUpdateItem =>
                    new ClientMessage.ReqUpdateItem(Guid.NewGuid(), RandomItemDtoGenerator.Generate()),

                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            string serialized = ClientMessageStringSerializer.Serialize(original);
            ClientMessage deserialized = ClientMessageStringSerializer.Deserialize(serialized);

            Assert.Equivalent(original, deserialized);
        }
    }

    /// <summary>
    /// Test that byte serialization and deserialization preserves the data
    /// </summary>
    /// <param name="type">One of <see cref="XUnitClientMessageType"/></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Theory]
    [InlineData(XUnitClientMessageType.ReqUpdate)]
    [InlineData(XUnitClientMessageType.ReqAddItem)]
    [InlineData(XUnitClientMessageType.ReqDeleteItem)]
    [InlineData(XUnitClientMessageType.ReqUpdateItem)]
    public void TestByte(XUnitClientMessageType type)
    {
        for (int i = 0; i < 50; i++)
        {
            ClientMessage original = type switch {
                XUnitClientMessageType.ReqUpdate =>
                    new ClientMessage.ReqUpdate(),

                XUnitClientMessageType.ReqAddItem =>
                    new ClientMessage.ReqAddItem(Guid.NewGuid(), RandomItemDtoGenerator.Generate()),

                XUnitClientMessageType.ReqDeleteItem =>
                    new ClientMessage.ReqDeleteItem(Guid.NewGuid()),

                XUnitClientMessageType.ReqUpdateItem =>
                    new ClientMessage.ReqUpdateItem(Guid.NewGuid(), RandomItemDtoGenerator.Generate()),

                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            byte[] serialized = new ClientMessageByteSerializer().Serialize(original);
            ClientMessage deserialized = new ClientMessageByteSerializer().Deserialize(serialized);

            Assert.Equivalent(original, deserialized);
        }
    }
}
