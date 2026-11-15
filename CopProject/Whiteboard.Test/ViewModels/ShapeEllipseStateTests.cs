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

public class ShapeEllipseStateTests
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
        Assert.Throws<ArgumentNullException>(() => new ShapeEllipseState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeEllipseState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeEllipseState(_client, _preview, null!));
    }

    /// <summary>
    /// Check that pressing the pointer starts an ellipse and shows the preview
    /// </summary>
    [Fact]
    public void PointerPressed_InitializesEllipseAndShowsPreview()
    {
        ShapeEllipseState state = new(_client, _preview, _toolbar);
        Point2D start = new(100, 150);

        state.PointerPressed(start);

        Assert.Single(_preview.ShownItems);
        IEllipse ellipse = Assert.IsAssignableFrom<IEllipse>(_preview.LastShownItem);
        Assert.Equal(start.X, ellipse.Center.X);
        Assert.Equal(start.Y, ellipse.Center.Y);
        Assert.Equal(0, ellipse.Width);
        Assert.Equal(0, ellipse.Height);
        Assert.Equal(_toolbar.BorderColor, ellipse.BorderColor);
        Assert.Equal(_toolbar.BorderWidth, ellipse.BorderWidth);
        Assert.Equal(_toolbar.FillColor, ellipse.FillColor);
        Assert.Equal(_client.UserId, ellipse.CreatedBy);
        Assert.Equal(0, ellipse.Zindex);
    }

    /// <summary>
    /// Check that the ellipse gets the next z-index when other items exist
    /// </summary>
    [Fact]
    public void PointerPressed_AssignsTopZIndexWhenItemsExist()
    {
        _client.Canvas.AddItem(new Circle { Zindex = 4 });
        _client.Canvas.AddItem(new Square { Zindex = 18 });

        ShapeEllipseState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(0, 0));

        IEllipse ellipse = Assert.IsAssignableFrom<IEllipse>(_preview.LastShownItem);
        Assert.Equal(19, ellipse.Zindex);
    }

    /// <summary>
    /// Check the ellipse size and center in all four drag directions
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
        ShapeEllipseState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(startX, startY));
        state.PointerMoved(new Point2D(moveX, moveY));

        Assert.Equal(2, _preview.ShownItems.Count);
        IEllipse ellipse = Assert.IsAssignableFrom<IEllipse>(_preview.LastShownItem);
        Assert.Equal(expectedWidth, ellipse.Width);
        Assert.Equal(expectedHeight, ellipse.Height);
        Assert.Equal(expectedCenterX, ellipse.Center.X);
        Assert.Equal(expectedCenterY, ellipse.Center.Y);
    }

    /// <summary>
    /// Check that moving the pointer before drawing does nothing
    /// </summary>
    [Fact]
    public void PointerMoved_WhenNotDrawing_DoesNothing()
    {
        ShapeEllipseState state = new(_client, _preview, _toolbar);

        state.PointerMoved(new Point2D(20, 20));

        Assert.Empty(_preview.ShownItems);
    }

    /// <summary>
    /// Check that a valid ellipse is added and the preview is cleared
    /// </summary>
    [Fact]
    public void PointerReleased_WithPositiveDimensions_SubmitsAndClearsPreview()
    {
        ShapeEllipseState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));
        state.PointerMoved(new Point2D(30, 40));

        state.PointerReleased(new Point2D(30, 40));

        Assert.Equal(1, _preview.ClearCount);
        Assert.True(_client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(2)), "Item was not added in time.");
        IEllipse submitted = Assert.IsAssignableFrom<IEllipse>(Assert.Single(_client.AddedItems));
        Assert.Equal(20, submitted.Width);
        Assert.Equal(30, submitted.Height);
    }

    /// <summary>
    /// Check that an empty ellipse is not added
    /// </summary>
    [Fact]
    public void PointerReleased_WithZeroDimension_DoesNotSubmit()
    {
        ShapeEllipseState state = new(_client, _preview, _toolbar);
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
        ShapeEllipseState state = new(_client, _preview, _toolbar);

        state.PointerReleased(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    /// <summary>
    /// Check that losing the pointer clears the preview and does not add an ellipse
    /// </summary>
    [Fact]
    public void PointerCaptureLost_WhenDrawing_ClearsPreviewWithoutSubmitting()
    {
        ShapeEllipseState state = new(_client, _preview, _toolbar);
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
        ShapeEllipseState state = new(_client, _preview, _toolbar);

        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }
}
