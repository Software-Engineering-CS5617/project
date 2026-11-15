// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/****
    * Filename  : HistoryMessageTests.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/


using System;
using Whiteboard.Items.Shapes;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;
using Xunit;

namespace Whiteboard.Test.Serialization;

/// <summary>
/// Tests sending undo, redo, and user history through message serialization.
/// </summary>
public class HistoryMessageTests
{
    /// <summary>
    /// Checks that undo and redo requests preserve their user and message type after serialization.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UndoRedoRequestPreservesUser(bool undo)
    {
        ClientMessage request = undo
            ? new ClientMessage.ReqUndo()
            : new ClientMessage.ReqRedo();

        request = request with { UserId = "Host" };

        var serializer = new ClientMessageByteSerializer();
        var restored = serializer.Deserialize(serializer.Serialize(request));

        Assert.Equal("Host", restored.UserId);

        if (undo)
        {
            Assert.IsType<ClientMessage.ReqUndo>(restored);
        }
        else
        {
            Assert.IsType<ClientMessage.ReqRedo>(restored);
        }
    }

    /// <summary>
    /// Checks that a history message preserves its user, entry order, and saved item states.
    /// </summary>
    [Fact]
    public void HistoryMessagePreservesEntriesAndOrder()
    {
        var first = ItemMapper.ToDto(new Square());
        var second = ItemMapper.ToDto(new Square());

        // An addition has no previous state; a deletion has no next state.
        var message = new ServerMessage.ResHistory(
            "Client",
            [
                new HistoryEntryDto(first.Metadata.Id, null, first),
                new HistoryEntryDto(second.Metadata.Id, second, null)
            ],
            []);

        var serializer = new ServerMessageByteSerializer();
        var restored = Assert.IsType<ServerMessage.ResHistory>(
            serializer.Deserialize(serializer.Serialize(message)));

        Assert.Equal("Client", restored.UserId);
        Assert.Equal(2, restored.Undo.Count);
        Assert.Equal(first.Metadata.Id, restored.Undo[0].ItemId);
        Assert.Equal(second.Metadata.Id, restored.Undo[1].ItemId);
        Assert.Null(restored.Undo[0].Before);
        Assert.Null(restored.Undo[1].After);
        Assert.Empty(restored.Redo);

        Assert.Equal(
            ServerMessageStringSerializer.Serialize(message),
            ServerMessageStringSerializer.Serialize(restored));
    }
}

