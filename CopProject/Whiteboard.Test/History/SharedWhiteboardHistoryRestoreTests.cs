using System.Collections.Generic;
using System.IO;
using System.Linq;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Models;
using Whiteboard.Services;
using Xunit;

namespace Whiteboard.Test.History;

public class SharedWhiteboardHistoryRestoreTests
{
    [Fact]
    public void RestoreTests_ReplaceCanvas()
    {
        var canvas = new WhiteboardCanvas();
        var history = new SharedWhiteboardHistoryService(canvas);

        var itemUser1 = new FreehandItem();
        itemUser1.Points.Add(new Point2D(1, 2));
        history.AddItem("User1", itemUser1);

        var itemUser2 = new FreehandItem();
        itemUser2.Points.Add(new Point2D(3, 4));
        history.AddItem("User2", itemUser2);

        history.Undo("User1");

        Assert.True(history.GetHistorySnapshots().Single(state => state.UserId == "User1").Redo.Count() > 0);

        var restoredItem = new FreehandItem();
        restoredItem.Points.Add(new Point2D(10, 20));

        history.RestoreState("User1", new IItem[] { restoredItem });

        Assert.Single(canvas.Items);
        Assert.Contains(restoredItem.Id, canvas.Items.Keys);
        Assert.DoesNotContain(itemUser1.Id, canvas.Items.Keys);
        Assert.DoesNotContain(itemUser2.Id, canvas.Items.Keys);

        var histories = history.GetHistorySnapshots().ToDictionary(state => state.UserId);

        Assert.Contains("User1", histories);
        Assert.Contains("User2", histories);

        foreach (var state in histories.Values)
        {
            Assert.Empty(state.Undo);
            Assert.Empty(state.Redo);
        }

        var restoredCanvas = Assert.IsType<FreehandItem>(canvas.Items[restoredItem.Id]);

        restoredItem.Points[0] = new Point2D(999, 999);

        Assert.Equal(new Point2D(10, 20), restoredCanvas.Points[0]);
        Assert.NotSame(restoredItem.Points, restoredCanvas.Points);
    }

    [Fact]
    public void RestoreTests_DuplicateItem()
    {
        var canvas = new WhiteboardCanvas();
        var history = new SharedWhiteboardHistoryService(canvas);

        var newItem = new FreehandItem();
        newItem.Points.Add(new Point2D(1, 2));
        history.AddItem("User1", newItem);

        var firstDuplicate = new FreehandItem();
        var secondDuplicate = new FreehandItem { Id = firstDuplicate.Id };

        Assert.Throws<InvalidDataException>(() => history.RestoreState("Bob", new IItem[] { firstDuplicate, secondDuplicate }));

        Assert.Single(canvas.Items);
        Assert.Contains(newItem.Id, canvas.Items.Keys);

        var user1History = history.GetHistorySnapshots().Single(state => state.UserId == "User1");

        Assert.Single(user1History.Undo);
    }
}