/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;

namespace Whiteboard.Test.Models;

public class ModelTests
{
    [Fact]
    public void CanvasExportData_Properties_SetAndGetCorrectly()
    {
        DateTime now = DateTime.UtcNow;
        byte[] bytes = [10, 20, 30];

        CanvasExportData data = new() {
            ImageData = bytes,
            ContentType = "image/png",
            FileName = "canvas.png",
            PixelWidth = 800,
            PixelHeight = 600,
            ExportDateTime = now
        };

        Assert.Equal(bytes, data.ImageData);
        Assert.Equal("image/png", data.ContentType);
        Assert.Equal("canvas.png", data.FileName);
        Assert.Equal(800, data.PixelWidth);
        Assert.Equal(600, data.PixelHeight);
        Assert.Equal(now, data.ExportDateTime);
    }

    [Fact]
    public void WhiteboardCanvas_AddItem_EditItem_DeleteItem_Clear()
    {
        WhiteboardCanvas canvas = new();
        Rectangle rect = new() { Center = new Point2D(10, 10), Width = 20, Height = 20 };

        bool addedRaised = false;
        bool updatedRaised = false;
        bool deletedRaised = false;

        canvas.ItemAdded += item => addedRaised = (item.Id == rect.Id);
        canvas.ItemUpdated += item => updatedRaised = (item.Id == rect.Id);
        canvas.ItemDeleted += id => deletedRaised = (id == rect.Id);

        // Add
        canvas.AddItem(rect);
        Assert.True(addedRaised);
        Assert.Single(canvas.Items);

        // Edit
        Rectangle editedRect = new() { Id = rect.Id, Center = new Point2D(30, 30), Width = 40, Height = 40 };
        Assert.True(canvas.EditItem(editedRect));
        Assert.True(updatedRaised);
        Assert.Equal(30, ((Rectangle)canvas.Items[rect.Id]).Center.X);

        // Hit testing
        Assert.NotNull(canvas.HitTestTopmost(new Point2D(30, 30)));
        Assert.Single(canvas.HitTestAll(new Point2D(30, 30)));
        Assert.Single(canvas.InRect(new Rect2D(0, 0, 100, 100)));

        // RemoveItem
        Assert.True(canvas.RemoveItem(editedRect));
        Assert.True(deletedRaised);
        Assert.Empty(canvas.Items);
        Assert.False(canvas.DeleteItem(rect.Id)); // already deleted
        Assert.Empty(canvas.Items);
    }

    [Fact]
    public void Rect2D_Properties_And_Calculations()
    {
        Rect2D rect = new(10, 20, 40, 50);

        Assert.Equal(10, rect.Left);
        Assert.Equal(20, rect.Top);
        Assert.Equal(40, rect.Width);
        Assert.Equal(50, rect.Height);

        Rect2D rectSame = new(10, 20, 40, 50);
        Assert.Equal(rect, rectSame);
    }

    [Fact]
    public void Point2D_Properties_And_Equality()
    {
        Point2D pt1 = new(15.5, 25.5);
        Point2D pt2 = new(15.5, 25.5);
        Point2D pt3 = pt1 with { X = 30.0 };

        Assert.Equal(15.5, pt1.X);
        Assert.Equal(25.5, pt1.Y);
        Assert.Equal(pt1, pt2);
        Assert.NotEqual(pt1, pt3);
        Assert.Equal(30.0, pt3.X);
    }

    [Fact]
    public void WhiteboardCanvas_NullItems_AreHandled()
    {
        // check how the canvas handles null items.
        WhiteboardCanvas canvas = new();

        Assert.Throws<ArgumentNullException>(() => canvas.AddItem(null!));
        Assert.Throws<ArgumentNullException>(() => canvas.EditItem(null!));
        Assert.False(canvas.RemoveItem(null!));
    }

    [Fact]
    public void WhiteboardCanvas_SelectionQueries_OrderByDescendingZIndex()
    {
        // check that higher z-index items come first.
        WhiteboardCanvas canvas = new();
        Rectangle lower = new() {
            Center = new Point2D(0, 0),
            Width = 20,
            Height = 20,
            Zindex = 1
        };
        Rectangle upper = new() {
            Center = new Point2D(0, 0),
            Width = 20,
            Height = 20,
            Zindex = 2
        };
        canvas.AddItem(lower);
        canvas.AddItem(upper);

        List<IItem> hits = canvas.HitTestAll(new Point2D(0, 0));
        List<IItem> overlaps = canvas.InRect(new Rect2D(-5, -5, 10, 10));

        Assert.Equal(upper.Id, hits[0].Id);
        Assert.Equal(lower.Id, hits[1].Id);
        Assert.Equal(upper.Id, overlaps[0].Id);
        Assert.Equal(lower.Id, overlaps[1].Id);
    }
}
