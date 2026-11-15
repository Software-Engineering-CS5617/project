/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;

namespace Whiteboard.Test.Serialization.NullOrEmptyEdgeCases;

public class ClientMessageSerializer_NOEEC
{
    /// <summary>
    /// Test that string serialization returns <see cref="string.Empty"/> when <see langword="null"/> is passed.
    /// </summary>
    [Fact]
    public void TestString()
    {
        // 1. Act
        string serialized = ClientMessageStringSerializer.Serialize(null);

        // 2. Assert
        Assert.Equivalent(serialized, "");
    }

    /// <summary>
    /// Test that byte serialization returns an empty byte sequence when <see langword="null"/> is passed.
    /// </summary>
    [Fact]
    public void TestByte()
    {
        // 1. Act
        byte[] serialized = new ClientMessageByteSerializer().Serialize(null);

        // 2. Assert
        Assert.Empty(serialized);
    }
}


public class ClientMessageDeserializer_NOEEC
{
    /// <summary>
    /// Test that string deserialization returns <see langword="null"/> when <see cref="string.Empty"/> is passed.
    /// </summary>
    [Fact]
    public void TestString()
    {
        // 1. Act
        ClientMessage deserialized = ClientMessageStringSerializer.Deserialize("");

        // 2. Assert
        Assert.Null(deserialized);
    }

    /// <summary>
    /// Test that byte deserialization returns <see langword="null"/> sequence when an empty byute sequence is passed.
    /// </summary>
    [Fact]
    public void TestByte()
    {
        // 1. Act
        ClientMessage deserialized = new ClientMessageByteSerializer().Deserialize([]);

        // 2. Assert
        Assert.Null(deserialized);
    }
}
