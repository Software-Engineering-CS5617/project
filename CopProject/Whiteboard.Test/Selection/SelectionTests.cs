using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Whiteboard.Selection;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.Selection;

public class SelectionTests
{
    private static readonly ToolbarInfo s_toolbar = new(
        Color.FromArgb(255, 0, 0, 0),
        2,
        Color.FromArgb(255, 255, 255, 255));

    [Fact]
    public void SelectionState_SelectToggleAndClear()
    {
        // check the basic selection changes.
        SelectionState state = new();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        state.SelectSingle(first);
        state.Toggle(second);
        state.Toggle(second);
        state.Clear();

        Assert.Empty(state.SelectedItemIds);
        Assert.Null(state.PrimarySelectedId);
        Assert.False(state.IsSelected(first));
    }

    [Fact]
    public void SelectionState_TogglingNonPrimaryItemKeepsPrimarySelection()
    {
        // remove a non-primary item.
        SelectionState state = new();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        state.SelectSingle(first);
        state.Toggle(second);
        state.Toggle(first);

        Assert.DoesNotContain(first, state.SelectedItemIds);
        Assert.Equal(second, state.PrimarySelectedId);
    }

    [Fact]
    public void SelectionService_SelectsHitClearsMissAndMovesItem()
    {
        // select and move the rectangle.
        WhiteboardCanvas canvas = new();
        Rectangle rectangle = new() {
            Center = new Point2D(10, 10),
            Width = 10,
            Height = 10
        };
        canvas.AddItem(rectangle);
        SelectionState state = new();
        SelectionService service = new(canvas, state);

        Assert.True(service.SelectAtPoint(rectangle.Center));
        Assert.Equal(rectangle.Id, state.PrimarySelectedId);
        Assert.True(service.MoveSelected(5, 2));
        Assert.False(service.SelectAtPoint(new Point2D(100, 100)));
        Assert.Null(state.PrimarySelectedId);
        Assert.Equal(new Point2D(15, 12), ((Rectangle)canvas.Items[rectangle.Id]).Center);
    }

    [Fact]
    public void SelectionService_MissingSelectedItemReturnsFalse()
    {
        // a missing item cannot be moved.
        SelectionState state = new();
        state.SelectSingle(Guid.NewGuid());

        Assert.False(new SelectionService(new WhiteboardCanvas(), state).MoveSelected(1, 1));
    }

    [Fact]
    public void SelectState_DragSendsOneEditAfterRelease()
    {
        // drag the item and release it.
        MockWhiteboardClient client = new();
        Rectangle rectangle = new() {
            Center = new Point2D(10, 10),
            Width = 10,
            Height = 10
        };
        client.AddItem(rectangle);
        SelectState state = new(client, new MockCanvasPreview(), s_toolbar);
        int selectionChanges = 0;
        state.SelectionChanged += () => selectionChanges++;

        state.PointerPressed(rectangle.Center);
        state.PointerMoved(new Point2D(20, 25));
        state.PointerReleased(new Point2D(20, 25));

        Assert.Equal(rectangle.Id, state.SelectedItemId);
        Assert.Equal(1, selectionChanges);
        Assert.Single(client.EditedItems);
        Assert.Equal(new Point2D(20, 25), ((Rectangle)client.Canvas.Items[rectangle.Id]).Center);
    }

    [Fact]
    public void SelectState_ClickingEmptyCanvasDoesNotSelect()
    {
        // an empty point keeps the canvas unselected.
        SelectState state = new(new MockWhiteboardClient(), new MockCanvasPreview(), s_toolbar);
        int selectionChanges = 0;
        state.SelectionChanged += () => selectionChanges++;

        state.PointerPressed(new Point2D(100, 100));

        Assert.Null(state.SelectedItemId);
        Assert.Equal(0, selectionChanges);
    }

    [Fact]
    public void SelectState_CaptureLossRestoresDraggedItem()
    {
        // restore the item when the pointer capture is lost.
        MockWhiteboardClient client = new();
        Rectangle rectangle = new() {
            Center = new Point2D(10, 10),
            Width = 10,
            Height = 10
        };
        client.AddItem(rectangle);
        SelectState state = new(client, new MockCanvasPreview(), s_toolbar);

        state.PointerPressed(rectangle.Center);
        state.PointerMoved(new Point2D(20, 25));
        state.PointerCaptureLost(new Point2D(20, 25));

        Assert.Equal(new Point2D(10, 10), ((Rectangle)client.Canvas.Items[rectangle.Id]).Center);
        Assert.Empty(client.EditedItems);
    }

    [Fact]
    public void SelectState_ClearSelectionAndDeletionUpdateSelection()
    {
        // clear selection and handle deleted items.
        MockWhiteboardClient client = new();
        Rectangle first = new() { Center = new Point2D(10, 10), Width = 10, Height = 10 };
        Rectangle second = new() { Center = new Point2D(30, 30), Width = 10, Height = 10 };
        client.AddItem(first);
        client.AddItem(second);
        SelectState state = new(client, new MockCanvasPreview(), s_toolbar);

        state.PointerPressed(first.Center);
        state.ClearSelection();
        state.ClearSelection();
        state.PointerPressed(first.Center);
        client.Canvas.DeleteItem(second.Id);

        Assert.Equal(first.Id, state.SelectedItemId);

        client.Canvas.DeleteItem(first.Id);

        Assert.Null(state.SelectedItemId);
    }
}
