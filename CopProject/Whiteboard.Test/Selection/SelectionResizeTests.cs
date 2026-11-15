/// Owner   : Sasi Kiran
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Whiteboard.Selection;

namespace Whiteboard.Test.Selection;

/// <summary>
/// Verifies resizing behavior for selected whiteboard items.
/// </summary>
public class SelectionResizeTests
{
    /// <summary>
    /// Verifies that a rectangle is resized from its top-left anchor.
    /// </summary>
    [Fact]
    public void ResizeSelected_ScalesRectangleFromTopLeftAnchor()
    {
        WhiteboardCanvas canvas = new();
        Rectangle rectangle = new() {
            Center = new Point2D(50, 40),
            Width = 40,
            Height = 20
        };
        canvas.AddItem(rectangle);

        SelectionState selection = new();
        selection.SelectSingle(rectangle.Id);
        SelectionService service = new(canvas, selection);

        bool resized = service.ResizeSelected(rectangle, new Point2D(80, 70));
        Rectangle result = (Rectangle)canvas.Items[rectangle.Id];

        Assert.True(resized);
        Assert.Equal(50, result.Width);
        Assert.Equal(40, result.Height);
        Assert.Equal(30, result.Left);
        Assert.Equal(30, result.Top);
    }

    /// <summary>
    /// Verifies that resizing scales every point in a freehand item.
    /// </summary>
    [Fact]
    public void ResizeSelected_ScalesFreehandPoints()
    {
        WhiteboardCanvas canvas = new();
        FreehandItem freehand = new() {
            Points = [new Point2D(10, 20), new Point2D(30, 40)]
        };
        canvas.AddItem(freehand);

        SelectionState selection = new();
        selection.SelectSingle(freehand.Id);
        SelectionService service = new(canvas, selection);

        bool resized = service.ResizeSelected(freehand, new Point2D(50, 80));
        FreehandItem result = (FreehandItem)canvas.Items[freehand.Id];

        Assert.True(resized);
        Assert.Equal(new Point2D(10, 20), result.Points[0]);
        Assert.Equal(new Point2D(50, 80), result.Points[1]);
    }
}
