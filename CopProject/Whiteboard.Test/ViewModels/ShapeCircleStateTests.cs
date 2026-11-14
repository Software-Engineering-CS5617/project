/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

public class ShapeCircleStateTests
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
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 7, 8, 9), 3.0, Color.FromArgb(255, 11, 12, 13));

    /// <summary>
    /// Assert that constructor throws <see cref="ArgumentNullException"/> if any of the arguments is null
    /// </summary>
    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ShapeCircleState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeCircleState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeCircleState(_client, _preview, null!));
    }

    /// <summary>
    /// Assert that pressing the mouse pointer at position (X, Y) initiates the circle correctly
    /// </summary>
    [Fact]
    public void PointerPressed_InitializesCircleAndShowsPreview()
    {
        ShapeCircleState state = new(_client, _preview, _toolbar);
        Point2D start = new(100, 150);

        state.PointerPressed(start);

        Assert.Single(_preview.ShownItems);
        ICircle circle = Assert.IsAssignableFrom<ICircle>(_preview.LastShownItem);
        Assert.Equal(start.X, circle.Center.X);
        Assert.Equal(start.Y, circle.Center.Y);
        Assert.Equal(0, circle.Radius);
        Assert.Equal(_toolbar.BorderColor, circle.BorderColor);
        Assert.Equal(_toolbar.BorderWidth, circle.BorderWidth);
        Assert.Equal(_toolbar.FillColor, circle.FillColor);
        Assert.Equal(_client.UserId, circle.CreatedBy);
        Assert.Equal(0, circle.Zindex);
    }

    /// <summary>
    /// Assert that the Z index is correctly instantiated when items are present on the whiteboard
    /// </summary>
    [Fact]
    public void PointerPressed_AssignsTopZIndexWhenItemsExist()
    {
        _client.Canvas.AddItem(new Circle { Zindex = 4 });
        _client.Canvas.AddItem(new Square { Zindex = 18 });

        ShapeCircleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(0, 0));

        ICircle circle = Assert.IsAssignableFrom<ICircle>(_preview.LastShownItem);
        Assert.Equal(19, circle.Zindex);
    }

    /// <summary>
    /// Assert that the circle is setup correctly regardless of where the pointer is with respect to the starting positin 
    /// </summary>
    [Theory]
    [InlineData(10, 10, 30, 30, 10, 20, 20)] // Bottom Right
    [InlineData(50, 10, 30, 30, 10, 40, 20)] // Bottom Left
    [InlineData(10, 50, 30, 30, 10, 20, 40)] // Top Right
    [InlineData(50, 50, 30, 30, 10, 40, 40)] // Top Left
    public void PointerMoved_ComputesCorrectQuadrantCenterAndRadius(
        double startX, double startY,
        double moveX, double moveY,
        double expectedRadius, double expectedCenterX, double expectedCenterY
    )
    {
        ShapeCircleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(startX, startY));
        state.PointerMoved(new Point2D(moveX, moveY));

        Assert.Equal(2, _preview.ShownItems.Count);
        ICircle circle = Assert.IsAssignableFrom<ICircle>(_preview.LastShownItem);
        Assert.Equal(expectedRadius, circle.Radius);
        Assert.Equal(expectedCenterX, circle.Center.X);
        Assert.Equal(expectedCenterY, circle.Center.Y);
    }

    /// <summary>
    /// Assert that moving the pointer when drawing does nothing
    /// </summary>
    [Fact]
    public void PointerMoved_WhenNotDrawing_DoesNothing()
    {
        ShapeCircleState state = new(_client, _preview, _toolbar);
        state.PointerMoved(new Point2D(20, 20));

        Assert.Empty(_preview.ShownItems);
    }

    /// <summary>
    /// Ensure the core functionality of circle, sending to client and clearing preview works
    /// </summary>
    [Fact]
    public void PointerReleased_WithPositiveRadius_SubmitsAndClearsPreview()
    {
        ShapeCircleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));
        state.PointerMoved(new Point2D(30, 30));

        state.PointerReleased(new Point2D(30, 30));

        Assert.Equal(1, _preview.ClearCount);

        bool wasAdded = _client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.True(wasAdded, "Item was not added in time.");

        Assert.Single(_client.AddedItems);
        ICircle submitted = Assert.IsAssignableFrom<ICircle>(_client.AddedItems[0]);
        Assert.Equal(10, submitted.Radius);
    }

    /// <summary>
    /// Assert that clicking a zero radius circle is not submitted
    /// </summary>
    [Fact]
    public void PointerReleased_WithZeroRadius_DoesNotSubmit()
    {
        ShapeCircleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));

        state.PointerReleased(new Point2D(10, 10));

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    /// <summary>
    /// Assert that pointer released, when not drawing, does nothing
    /// </summary>
    [Fact]
    public void PointerReleased_WhenNotDrawing_DoesNothing()
    {
        ShapeCircleState state = new(_client, _preview, _toolbar);
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
        ShapeCircleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));
        state.PointerMoved(new Point2D(30, 30));

        state.PointerCaptureLost(new Point2D(30, 30));

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);

        state.PointerMoved(new Point2D(50, 50));
        Assert.Equal(2, _preview.ShownItems.Count);
    }

    /// <summary>
    /// Assert that losing pointer when not drawing does not do anything
    /// </summary>
    [Fact]
    public void PointerCaptureLost_WhenNotDrawing_DoesNothing()
    {
        ShapeCircleState state = new(_client, _preview, _toolbar);
        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
    }
}
