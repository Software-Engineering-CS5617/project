/// Owner   : Sasi Kiran
/// Roll no : 112301033
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

public class ShapeRectangleStateTests
{
    /// <summary>
    /// Fake whiteboard client used by the tests
    /// </summary>
    private readonly MockWhiteboardClient _client = new();

    /// <summary>
    /// Fake canvas preview used by the tests
    /// </summary>
    private readonly MockCanvasPreview _preview = new();

    /// <summary>
    /// Toolbar settings used by the tests
    /// </summary>
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 7, 8, 9), 3.0, Color.FromArgb(255, 11, 12, 13));

    /// <summary>
    /// Check that the constructor rejects null arguments
    /// </summary>
    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ShapeRectangleState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeRectangleState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeRectangleState(_client, _preview, null!));
    }

    /// <summary>
    /// Check that pressing the pointer starts a rectangle and shows the preview
    /// </summary>
    [Fact]
    public void PointerPressed_InitializesRectangleAndShowsPreview()
    {
        ShapeRectangleState state = new(_client, _preview, _toolbar);
        Point2D start = new(100, 150);

        state.PointerPressed(start);

        Assert.Single(_preview.ShownItems);
        IRectangle rectangle = Assert.IsAssignableFrom<IRectangle>(_preview.LastShownItem);
        Assert.Equal(start.X, rectangle.Center.X);
        Assert.Equal(start.Y, rectangle.Center.Y);
        Assert.Equal(0, rectangle.Width);
        Assert.Equal(0, rectangle.Height);
        Assert.Equal(_toolbar.BorderColor, rectangle.BorderColor);
        Assert.Equal(_toolbar.BorderWidth, rectangle.BorderWidth);
        Assert.Equal(_toolbar.FillColor, rectangle.FillColor);
        Assert.Equal(_client.UserId, rectangle.CreatedBy);
        Assert.Equal(0, rectangle.Zindex);
    }

    /// <summary>
    /// Check that the rectangle gets the next z-index when other items exist
    /// </summary>
    [Fact]
    public void PointerPressed_AssignsTopZIndexWhenItemsExist()
    {
        _client.Canvas.AddItem(new Circle { Zindex = 4 });
        _client.Canvas.AddItem(new Square { Zindex = 18 });

        ShapeRectangleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(0, 0));

        IRectangle rectangle = Assert.IsAssignableFrom<IRectangle>(_preview.LastShownItem);
        Assert.Equal(19, rectangle.Zindex);
    }

    /// <summary>
    /// Check the rectangle size and center in all four drag directions
    /// </summary>
    [Theory]
    [InlineData(10, 10, 30, 40, 20, 30, 20, 25)] // Bottom Right
    [InlineData(50, 10, 30, 40, 20, 30, 40, 25)] // Bottom Left
    [InlineData(10, 50, 30, 40, 20, 10, 20, 45)] // Top Right
    [InlineData(50, 50, 30, 40, 20, 10, 40, 45)] // Top Left
    public void PointerMoved_ComputesCorrectQuadrantCenterAndDimensions(
        double startX, double startY,
        double moveX, double moveY,
        double expectedWidth, double expectedHeight,
        double expectedCenterX, double expectedCenterY)
    {
        ShapeRectangleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(startX, startY));
        state.PointerMoved(new Point2D(moveX, moveY));

        Assert.Equal(2, _preview.ShownItems.Count);
        IRectangle rectangle = Assert.IsAssignableFrom<IRectangle>(_preview.LastShownItem);
        Assert.Equal(expectedWidth, rectangle.Width);
        Assert.Equal(expectedHeight, rectangle.Height);
        Assert.Equal(expectedCenterX, rectangle.Center.X);
        Assert.Equal(expectedCenterY, rectangle.Center.Y);
    }

    /// <summary>
    /// Check that moving the pointer before drawing does nothing
    /// </summary>
    [Fact]
    public void PointerMoved_WhenNotDrawing_DoesNothing()
    {
        ShapeRectangleState state = new(_client, _preview, _toolbar);

        state.PointerMoved(new Point2D(20, 20));

        Assert.Empty(_preview.ShownItems);
    }

    /// <summary>
    /// Check that a valid rectangle is added and the preview is cleared
    /// </summary>
    [Fact]
    public void PointerReleased_WithPositiveDimensions_SubmitsAndClearsPreview()
    {
        ShapeRectangleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));
        state.PointerMoved(new Point2D(30, 40));

        state.PointerReleased(new Point2D(30, 40));

        Assert.Equal(1, _preview.ClearCount);
        Assert.True(_client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(2)), "Item was not added in time.");
        IRectangle submitted = Assert.IsAssignableFrom<IRectangle>(Assert.Single(_client.AddedItems));
        Assert.Equal(20, submitted.Width);
        Assert.Equal(30, submitted.Height);
    }

    /// <summary>
    /// Check that an empty rectangle is not added
    /// </summary>
    [Fact]
    public void PointerReleased_WithZeroDimension_DoesNotSubmit()
    {
        ShapeRectangleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));

        state.PointerReleased(new Point2D(10, 10));

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    /// <summary>
    /// Check that releasing the pointer before drawing does nothing
    /// </summary>
    [Fact]
    public void PointerReleased_WhenNotDrawing_DoesNothing()
    {
        ShapeRectangleState state = new(_client, _preview, _toolbar);

        state.PointerReleased(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    /// <summary>
    /// Check that losing the pointer clears the preview and does not add a rectangle
    /// </summary>
    [Fact]
    public void PointerCaptureLost_WhenDrawing_ClearsPreviewWithoutSubmitting()
    {
        ShapeRectangleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));
        state.PointerMoved(new Point2D(30, 40));

        state.PointerCaptureLost(new Point2D(30, 40));

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    /// <summary>
    /// Check that losing the pointer before drawing does nothing
    /// </summary>
    [Fact]
    public void PointerCaptureLost_WhenNotDrawing_DoesNothing()
    {
        ShapeRectangleState state = new(_client, _preview, _toolbar);

        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }
}
