/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

public class ShapeEqTriangleStateTests
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
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 1, 2, 3), 2.0, Color.FromArgb(255, 4, 5, 6));

    /// <summary>
    /// Assert that constructor throws <see cref="ArgumentNullException"/> if any of the arguments is null
    /// </summary>
    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ShapeEqTriangleState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeEqTriangleState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeEqTriangleState(_client, _preview, null!));
    }

    /// <summary>
    /// Assert that pressing the mouse pointer at position (X, Y) initiates the triangle correctly
    /// </summary>
    [Fact]
    public void PointerPressed_InitializesTriangleAndShowsPreview()
    {
        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
        Point2D start = new(50, 60);

        state.PointerPressed(start);

        Assert.Single(_preview.ShownItems);
        IEqTriangle triangle = Assert.IsAssignableFrom<IEqTriangle>(_preview.LastShownItem);
        Assert.Equal(start.X, triangle.Center.X);
        Assert.Equal(start.Y, triangle.Center.Y);
        Assert.Equal(0, triangle.SideLength);
        Assert.Equal(_toolbar.BorderColor, triangle.BorderColor);
        Assert.Equal(_toolbar.BorderWidth, triangle.BorderWidth);
        Assert.Equal(_toolbar.FillColor, triangle.FillColor);
        Assert.Equal(_client.UserId, triangle.CreatedBy);
        Assert.Equal(0, triangle.Zindex);
    }

    /// <summary>
    /// Assert that the Z index is correctly instantiated when items are present on the whiteboard
    /// </summary>
    [Fact]
    public void PointerPressed_AssignsTopZIndexWhenItemsExist()
    {
        _client.Canvas.AddItem(new Square { Zindex = 10 });
        _client.Canvas.AddItem(new Circle { Zindex = 25 });

        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(0, 0));

        IEqTriangle triangle = Assert.IsAssignableFrom<IEqTriangle>(_preview.LastShownItem);
        Assert.Equal(26, triangle.Zindex);
    }

    /// <summary>
    /// Assert that the circle is setup correctly regardless of where the pointer is with respect to the starting positin 
    /// </summary>
    [Theory]
    [InlineData(10, 10, 30, 30)] // Bottom Right
    [InlineData(50, 10, 30, 30)] // Bottom Left
    [InlineData(10, 50, 30, 30)] // Top Right
    [InlineData(50, 50, 30, 30)] // Top Left
    public void PointerMoved_ComputesCorrectQuadrantCenterAndSide(
        double startX, double startY,
        double moveX, double moveY
    )
    {
        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(startX, startY));
        state.PointerMoved(new Point2D(moveX, moveY));

        Assert.Equal(2, _preview.ShownItems.Count);
        IEqTriangle triangle = Assert.IsAssignableFrom<IEqTriangle>(_preview.LastShownItem);

        double horizontal = Math.Abs(moveX - startX);
        double vertical = Math.Abs(moveY - startY);
        double expectedSide = Math.Min(horizontal, vertical * 2.0 / Math.Sqrt(3.0));
        double expectedHeight = expectedSide * Math.Sqrt(3.0) / 2.0;
        double expectedLeft = moveX >= startX ? startX : startX - expectedSide;
        double expectedTop = moveY >= startY ? startY : startY - expectedHeight;
        double expectedCenterX = expectedLeft + (expectedSide / 2.0);
        double expectedCenterY = expectedTop + ((2.0 / 3.0) * expectedHeight);

        Assert.Equal(expectedSide, triangle.SideLength);
        Assert.Equal(expectedCenterX, triangle.Center.X, precision: 5);
        Assert.Equal(expectedCenterY, triangle.Center.Y, precision: 5);
        Assert.Equal(expectedLeft, triangle.BoundingBox.Left, precision: 5);
        Assert.Equal(expectedTop, triangle.BoundingBox.Top, precision: 5);
    }

    /// <summary>
    /// Assert that moving the pointer when drawing does nothing
    /// </summary>
    [Fact]
    public void PointerMoved_WhenNotDrawing_DoesNothing()
    {
        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
        state.PointerMoved(new Point2D(20, 20));

        Assert.Empty(_preview.ShownItems);
    }

    /// <summary>
    /// Ensure the core functionality of triangle, sending to client and clearing preview works
    /// </summary>
    [Fact]
    public void PointerReleased_WithPositiveSide_SubmitsAndClearsPreview()
    {
        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));
        state.PointerMoved(new Point2D(30, 30));

        state.PointerReleased(new Point2D(30, 30));

        Assert.Equal(1, _preview.ClearCount);

        bool wasAdded = _client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.True(wasAdded, "Item was not added in time.");

        Assert.Single(_client.AddedItems);
        IEqTriangle submitted = Assert.IsAssignableFrom<IEqTriangle>(_client.AddedItems[0]);
        Assert.True(submitted.SideLength > 0);
    }

    /// <summary>
    /// Assert that clicking a zero sidelength triangle is not submitted
    /// </summary>
    [Fact]
    public void PointerReleased_WithZeroSide_DoesNotSubmit()
    {
        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
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
        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
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
        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
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
        ShapeEqTriangleState state = new(_client, _preview, _toolbar);
        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
    }
}
