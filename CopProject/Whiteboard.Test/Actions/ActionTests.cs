// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/****
    * Filename  : ActionTests.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/


using System;
using Whiteboard.Actions;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Xunit;

namespace Whiteboard.Test.Actions;

/// <summary>
/// Tests adding, deleting, and restoring items through whiteboard actions.
/// </summary>
public class ActionTests
{
    /// <summary>
    /// Checks that reversing a group addition removes all added items.
    /// </summary>
    [Fact]
    public void GroupAddCanBeReversed()
    {
        var board = new WhiteboardCanvas();
        var first = new Square();
        var second = new Square();
        var action = new AddItemAction(new IItem[] { first, second }, "Host");

        action.Perform(board);
        Assert.Equal(2, board.Items.Count);

        var inverse = action.CreateInverse();
        Assert.Equal("Host", inverse.UserId);

        inverse.Perform(board);
        Assert.Empty(board.Items);
    }

    /// <summary>
    /// Checks that duplicate and empty additions are rejected without changing the board.
    /// </summary>
    [Fact]
    public void InvalidAddDoesNotChangeBoard()
    {
        var board = new WhiteboardCanvas();
        var item = new Square();
        board.AddItem(item);

        var duplicate = new AddItemAction(item, "Host");

        Assert.Throws<InvalidOperationException>(() => duplicate.Perform(board));
        Assert.Single(board.Items);
        Assert.Throws<ArgumentException>(() =>
            new AddItemAction(Array.Empty<IItem>(), "Host"));
    }

    /// <summary>
    /// Checks that a missing item prevents group deletion and creation of an inverse action.
    /// </summary>
    [Fact]
    public void DeleteNeedsToSucceedBeforeCreatingInverse()
    {
        var board = new WhiteboardCanvas();
        var item = new Square();
        board.AddItem(item);

        var action = new DeleteItemsAction(new[] { item.Id, Guid.NewGuid() }, "Host");

        // One missing item should prevent the whole deletion.
        Assert.Throws<InvalidOperationException>(() => action.Perform(board));
        Assert.True(board.Items.ContainsKey(item.Id));
        Assert.Throws<InvalidOperationException>(() => action.CreateInverse());
    }

    /// <summary>
    /// Checks that undoing an erase restores the saved stroke after the original changes.
    /// </summary>
    [Fact]
    public void DrawAndEraseActionsPreserveStrokeForUndo()
    {
        var board = new WhiteboardCanvas();
        var stroke = new FreehandItem {
            Points = [new Point2D(10, 20), new Point2D(30, 40)],
            StrokeWidth = 3
        };

        new DrawFreehandAction(stroke, "Host").Perform(board);

        var erase = new EraseAction(board.Items[stroke.Id], "Host");
        erase.Perform(board);
        Assert.Empty(board.Items);

        // Changing the original stroke must not affect the saved copy.
        stroke.Points.Clear();
        erase.CreateInverse().Perform(board);

        var restored = Assert.IsType<FreehandItem>(board.Items[stroke.Id]);
        Assert.Equal(2, restored.Points.Count);
        Assert.Equal(3, restored.StrokeWidth);
    }
}

