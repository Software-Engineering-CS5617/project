// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/****
    * Filename  : HistoryTest.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/



using System;
using System.Collections.Generic;
using System.Linq;
using Whiteboard.Actions;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Models;
using Whiteboard.Services;
using Xunit;

using RectangleItem = Whiteboard.Items.Shapes.Rectangle;
using SquareItem = Whiteboard.Items.Shapes.Square;
using Windows.UI;

namespace Whiteboard.Test.History;

/// <summary>
/// Tests local and shared undo, redo, edits, and separate user histories.
/// </summary>
public class HistoryTests
{
    private const string User = "sangam";

    /// <summary>
    /// Checks that undo and redo preserve the added item's identity and data.
    /// </summary>
    [Fact]
    public void AddUndoRedoPreservesItemId()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);
        var item = new RectangleItem { Width = 100 };

        history.Execute(new AddItemAction(item, User));

        Assert.True(board.Items.ContainsKey(item.Id));
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        history.Undo();

        Assert.Empty(board.Items);
        Assert.True(history.CanRedo);

        history.Redo();

        RectangleItem restored = Assert.IsType<RectangleItem>(board.Items[item.Id]);
        Assert.Equal(100d, restored.Width);
    }

    /// <summary>
    /// Checks that a group deletion is undone and redone as one action.
    /// </summary>
    [Fact]
    public void GroupDeletionIsOneUndoStep()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        var first = new RectangleItem();
        var second = new RectangleItem();

        history.Execute(new AddItemAction(first, User));
        history.Execute(new AddItemAction(second, User));

        history.Execute(new DeleteItemsAction(
            new[] { first.Id, second.Id }, User));

        Assert.Empty(board.Items);

        history.Undo();

        Assert.Equal(2, board.Items.Count);

        history.Redo();

        Assert.Empty(board.Items);
    }

    /// <summary>
    /// Checks that a successful new action clears redo.
    /// </summary>
    [Fact]
    public void SuccessfulNewActionClearsRedo()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        history.Execute(new AddItemAction(new RectangleItem(), User));
        history.Undo();

        Assert.True(history.CanRedo);

        history.Execute(new AddItemAction(new RectangleItem(), User));

        Assert.False(history.CanRedo);
    }

    /// <summary>
    /// Checks that a failed group deletion preserves the board and redo.
    /// </summary>
    [Fact]
    public void MissingItemPreventsWholeGroupDeletionAndPreservesRedo()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);
        var first = new RectangleItem();
        var second = new RectangleItem();

        history.Execute(new AddItemAction(first, User));
        history.Execute(new AddItemAction(second, User));

        // Keep one item on the board and one action available for redo.
        history.Undo();

        Assert.Throws<InvalidOperationException>(() =>
            history.Execute(new DeleteItemsAction(
                new[] { first.Id, Guid.NewGuid() }, User)));

        Assert.True(board.Items.ContainsKey(first.Id));
        Assert.True(history.CanRedo);

        history.Redo();

        Assert.Equal(2, board.Items.Count);
    }

    /// <summary>
    /// Checks that a duplicate add does not overwrite data or enter history.
    /// </summary>
    [Fact]
    public void DuplicateAddDoesNotOverwriteOrCreateHistoryEntry()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        var first = new RectangleItem { Width = 100 };
        history.Execute(new AddItemAction(first, User));

        var duplicate = new RectangleItem {
            Id = first.Id,
            Width = 900
        };

        Assert.Throws<InvalidOperationException>(() =>
            history.Execute(new AddItemAction(duplicate, User)));

        RectangleItem current = Assert.IsType<RectangleItem>(board.Items[first.Id]);
        Assert.Equal(100d, current.Width);

        // Only the original successful add should need undoing.
        history.Undo();

        Assert.Empty(board.Items);
        Assert.False(history.CanUndo);
    }

    /// <summary>
    /// Checks that changing a removed stroke does not change its saved copy.
    /// </summary>
    [Fact]
    public void DeletedStrokeSnapshotIsIndependentOfRemovedObject()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        var stroke = new FreehandItem {
            Points = new List<Point2D>
            {
                new Point2D(10, 20),
                new Point2D(30, 40)
            },
            StrokeWidth = 3
        };

        history.Execute(new AddItemAction(stroke, User));

        FreehandItem boardStroke =
            Assert.IsType<FreehandItem>(board.Items[stroke.Id]);

        history.Execute(new DeleteItemsAction(stroke.Id, User));

        // The removed object can still be changed through an old reference.
        boardStroke.Points[0] = new Point2D(999, 999);
        boardStroke.Points.Add(new Point2D(500, 500));

        history.Undo();

        FreehandItem restored =
            Assert.IsType<FreehandItem>(board.Items[stroke.Id]);

        Assert.Equal(2, restored.Points.Count);
        Assert.Equal(new Point2D(10, 20), restored.Points[0]);
        Assert.NotSame(boardStroke.Points, restored.Points);
    }

    /// <summary>
    /// Checks that another user's action is rejected.
    /// </summary>
    [Fact]
    public void AnotherUsersActionIsRejected()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        Assert.Throws<InvalidOperationException>(() =>
            history.Execute(
                new AddItemAction(new RectangleItem(), "another-user")));

        Assert.Empty(board.Items);
        Assert.False(history.CanUndo);
    }

    /// <summary>
    /// Checks that undo and redo do nothing when history is empty.
    /// </summary>
    [Fact]
    public void EmptyUndoAndRedoDoNothing()
    {
        var board = new WhiteboardCanvas();
        var history = new WhiteboardHistoryService(board, User);

        history.Undo();
        history.Redo();

        Assert.Empty(board.Items);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    /// <summary>
    /// Checks that successful local operations notify the view once.
    /// </summary>
    [Fact]
    public void HistoryChangedAfterSuccessfulOperations()
    {
        var history = new WhiteboardHistoryService(new WhiteboardCanvas(), User);
        int notifications = 0;
        history.HistoryChanged += () => notifications++;

        history.Undo();
        Assert.Equal(0, notifications);

        history.Execute(new AddItemAction(new RectangleItem(), User));
        history.Undo();
        history.Redo();

        Assert.Equal(3, notifications);
    }

    /// <summary>
    /// Checks that host and client can undo their own independent drawings.
    /// </summary>
    [Fact]
    public void UsersHaveSeparateHistory()
    {
        var board = new WhiteboardCanvas();
        var history = new SharedWhiteboardHistoryService(board);
        var hostItem = new SquareItem();
        var clientItem = new SquareItem();

        history.AddItem("Host", hostItem);
        history.AddItem("Client", clientItem);
        history.Undo("Host");

        Assert.False(board.Items.ContainsKey(hostItem.Id));
        Assert.True(board.Items.ContainsKey(clientItem.Id));

        var host = history.GetHistorySnapshots().Single(state => state.UserId == "Host");
        var client = history.GetHistorySnapshots().Single(state => state.UserId == "Client");

        Assert.Empty(host.Undo);
        Assert.Single(host.Redo);
        Assert.Single(client.Undo);
        Assert.Empty(client.Redo);

        history.Redo("Host");
        Assert.Equal(2, board.Items.Count);
    }

    /// <summary>
    /// Checks color, movement, and size edits through undo and redo.
    /// </summary>
    [Fact]
    public void UndoRedoRestoresModifiedItem()
    {
        var board = new WhiteboardCanvas();
        var history = new SharedWhiteboardHistoryService(board);
        var red = Color.FromArgb(255, 255, 0, 0);
        var blue = Color.FromArgb(255, 0, 0, 255);
        var item = new SquareItem {
            Center = new Point2D(100, 100), SideLegth = 40, FillColor = red
        };

        history.AddItem("Host", item);

        // Edit a copy so the original state can still be saved for undo.
        var edited = (SquareItem)ItemSnapshot.Copy(board.Items[item.Id]);
        edited.FillColor = blue;
        edited.SideLegth = 80;
        edited.MoveBy(20, 10);
        history.EditItem("Host", edited);

        history.Undo("Host");
        var restored = Assert.IsType<SquareItem>(board.Items[item.Id]);
        Assert.Equal(red, restored.FillColor);
        Assert.Equal(40, restored.SideLegth);
        Assert.Equal(new Point2D(100, 100), restored.Center);

        history.Redo("Host");
        restored = Assert.IsType<SquareItem>(board.Items[item.Id]);
        Assert.Equal(blue, restored.FillColor);
        Assert.Equal(80, restored.SideLegth);
        Assert.Equal(new Point2D(120, 110), restored.Center);
    }

    /// <summary>
    /// Checks that another user's edit removes only conflicting history.
    /// </summary>
    [Fact]
    public void OtherUserEditRemovesOldUndoAndRedo()
    {
        var board = new WhiteboardCanvas();
        var history = new SharedWhiteboardHistoryService(board);
        var item = new SquareItem { SideLegth = 40 };
        var separateItem = new SquareItem();

        history.AddItem("Host", item);
        history.AddItem("Host", separateItem);

        var resized = (SquareItem)ItemSnapshot.Copy(board.Items[item.Id]);
        resized.SideLegth = 80;
        history.EditItem("Host", resized);
        history.Undo("Host");

        // Host now has an undo and a redo entry for the same item.
        var moved = ItemSnapshot.Copy(board.Items[item.Id]);
        moved.MoveBy(20, 10);
        history.EditItem("Client", moved);

        var host = history.GetHistorySnapshots().Single(state => state.UserId == "Host");
        Assert.Equal(separateItem.Id, Assert.Single(host.Undo).ItemId);
        Assert.Empty(host.Redo);
        Assert.Equal("Host", board.Items[item.Id].CreatedBy);

        history.Undo("Client");
        var restored = Assert.IsType<SquareItem>(board.Items[item.Id]);
        Assert.Equal(new Point2D(0, 0), restored.Center);
        Assert.Equal(40, restored.SideLegth);

        // Undoing the client's edit does not bring back the host's removed history.
        host = history.GetHistorySnapshots().Single(state => state.UserId == "Host");
        Assert.Equal(separateItem.Id, Assert.Single(host.Undo).ItemId);
        Assert.Empty(host.Redo);
    }

    /// <summary>
    /// Checks that the person deleting an item can undo and redo that deletion.
    /// </summary>
    [Fact]
    public void ClientCanUndoDeletingHostItem()
    {
        var board = new WhiteboardCanvas();
        var history = new SharedWhiteboardHistoryService(board);
        var item = new SquareItem();

        history.AddItem("Host", item);
        history.DeleteItem("Client", item.Id);
        Assert.Empty(board.Items);

        history.Undo("Client");
        Assert.Equal("Host", board.Items[item.Id].CreatedBy);
        Assert.False(history.GetHistoryStates().Single(state => state.UserId == "Host").CanUndo);

        history.Redo("Client");
        Assert.Empty(board.Items);
    }

    /// <summary>
    /// Checks that a new drawing clears only its user's redo stack.
    /// </summary>
    [Fact]
    public void NewActionClearsOnlyOwnRedo()
    {
        var history = new SharedWhiteboardHistoryService(new WhiteboardCanvas());

        history.AddItem("Host", new SquareItem());
        history.AddItem("Client", new SquareItem());
        history.Undo("Host");
        history.Undo("Client");

        history.AddItem("Host", new SquareItem());

        var host = history.GetHistorySnapshots().Single(state => state.UserId == "Host");
        var client = history.GetHistorySnapshots().Single(state => state.UserId == "Client");
        Assert.Single(host.Undo);
        Assert.Empty(host.Redo);
        Assert.Empty(client.Undo);
        Assert.Single(client.Redo);
    }
}
