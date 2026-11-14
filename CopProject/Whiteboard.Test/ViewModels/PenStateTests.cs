/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Windows.UI;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;

namespace Whiteboard.Test.ViewModels;

/// <summary>
/// Pen state tests
/// </summary>
public class PenStateTests
{
    /// <summary>
    /// Mock whiteboard client
    /// </summary>
    private readonly MockWhiteboardClient _client = new();

    /// <summary>
    /// Mock canvas previewer
    /// </summary>
    private readonly MockCanvasPreview _preview = new();

    /// <summary>
    /// A random toolbar
    /// </summary>
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 0, 0, 0), 3, Color.FromArgb(255, 255, 0, 0));

    /// <summary>
    /// Assert that constructor throws <see cref="ArgumentNullException"/> if any of the arguments is null
    /// </summary>
    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PenState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new PenState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new PenState(_client, _preview, null!));
    }

    /// <summary>
    /// Assert that pressing the mouse pointer at position (X, Y) initiates the pen correctly
    /// </summary>
    [Fact]
    public void PointerPressed_InitializesDrawingAndShowsPreview()
    {
        PenState state = new(_client, _preview, _toolbar);

        // Simulate pointer press
        Point2D start = new(10, 20);
        state.PointerPressed(start);

        // Assert that only a single item is shown so far
        Assert.Single(_preview.ShownItems);

        // Assert that the item shown implements IFreehand
        IFreehand freehand = Assert.IsAssignableFrom<IFreehand>(_preview.LastShownItem);
        // Assert that it has only single point on it
        Assert.Single(freehand.Points);

        // Assert that the single point matches the start point
        Assert.Equal(start, freehand.Points[0]);

        // Assert that the toolbar information matches
        Assert.Equal(_toolbar.BorderColor, freehand.StrokeColor);
        Assert.Equal(_toolbar.BorderWidth, freehand.StrokeWidth);

        // Assert that the user id matches
        Assert.Equal(_client.UserId, freehand.CreatedBy);

        // Assert that the index matches
        Assert.Equal(0, freehand.Zindex);
    }

    /// <summary>
    /// Asser that the Z index is correctly instantiated when items are present on the whiteboard
    /// </summary>
    [Fact]
    public void PointerPressed_AssignsTopZIndexWhenItemsExist()
    {
        // Add some items on the whiteboard
        _client.Canvas.AddItem(new Square { Zindex = 5 });
        _client.Canvas.AddItem(new Square { Zindex = 12 });

        PenState state = new(_client, _preview, _toolbar);

        // Simluate a pointer press event
        state.PointerPressed(new Point2D(0, 0));

        IFreehand freehand = Assert.IsAssignableFrom<IFreehand>(_preview.LastShownItem);

        // Assert its Z index matches (1 + max(5, 12))
        Assert.Equal(13, freehand.Zindex);
    }

    /// <summary>
    /// Assert that moving the pointer also updates the pen as expected
    /// </summary>
    [Fact]
    public void PointerMoved_WhenDrawing_AppendsPointsAndUpdatesPreview()
    {
        PenState state = new(_client, _preview, _toolbar);

        // Simulate pointer press, then two pointer move events
        state.PointerPressed(new Point2D(0, 0));
        state.PointerMoved(new Point2D(5, 10));
        state.PointerMoved(new Point2D(15, 20));

        // Assert that preview is called three times
        Assert.Equal(3, _preview.ShownItems.Count);

        IFreehand freehand = Assert.IsAssignableFrom<IFreehand>(_preview.LastShownItem);

        // Assert that the freehand item is the same as what we expect it to be
        Assert.Equal(3, freehand.Points.Count);
        Assert.Equal(new Point2D(15, 20), freehand.Points[2]);
    }

    /// <summary>
    /// Assert that moving the pointer when drawing does nothing
    /// </summary>
    [Fact]
    public void PointerMoved_WhenNotDrawing_DoesNothing()
    {
        PenState state = new(_client, _preview, _toolbar);

        state.PointerMoved(new Point2D(5, 10));

        Assert.Empty(_preview.ShownItems);
    }

    /// <summary>
    /// Ensure the core functionality of drawing, sending to client and clearing preview works
    /// </summary>
    [Fact]
    public void PointerReleased_WithAtLeastTwoPoints_SubmitsAndClearsPreview()
    {
        PenState state = new(_client, _preview, _toolbar);

        state.PointerPressed(new Point2D(0, 0));
        state.PointerMoved(new Point2D(10, 10));
        state.PointerReleased(new Point2D(10, 10));

        Assert.Equal(1, _preview.ClearCount);

        bool wasAdded = _client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(2)); // Wait for at most 2000ms
        Assert.True(wasAdded, "Item was not added in due time.");

        Assert.Single(_client.AddedItems);
        IFreehand submitted = Assert.IsAssignableFrom<IFreehand>(_client.AddedItems[0]);

        Assert.Equal(2, submitted.Points.Count);
    }

    /// <summary>
    /// Assert that clicking does not create a new stroke
    /// </summary>
    [Fact]
    public void PointerReleased_WithSinglePoint_DoesNotSubmit()
    {
        PenState state = new(_client, _preview, _toolbar);

        state.PointerPressed(new Point2D(0, 0));
        state.PointerReleased(new Point2D(0, 0));

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    /// <summary>
    /// Assert that pointer released, when not drawing, does nothing
    /// </summary>
    [Fact]
    public void PointerReleased_WhenNotDrawing_DoesNothing()
    {
        PenState state = new(_client, _preview, _toolbar);

        state.PointerReleased(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    /// <summary>
    /// Assert that losing the pointer when drawing clears the preview
    /// </summary>
    [Fact]
    public void PointerCaptureLost_WhenDrawing_ClearsPreviewWithoutSubmitting()
    {
        PenState state = new(_client, _preview, _toolbar);

        state.PointerPressed(new Point2D(0, 0));
        state.PointerMoved(new Point2D(10, 10));
        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);

        // Subsequent move does not do anything
        state.PointerMoved(new Point2D(20, 20));

        Assert.Equal(2, _preview.ShownItems.Count);
    }

    /// <summary>
    /// Assert that losing pointer when not drawing does not do anything
    /// </summary>
    [Fact]
    public void PointerCaptureLost_WhenNotDrawing_DoesNothing()
    {
        PenState state = new(_client, _preview, _toolbar);
        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
    }
}
