/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

public class ShapeSquareStateTests
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
        Assert.Throws<ArgumentNullException>(() => new ShapeSquareState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeSquareState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new ShapeSquareState(_client, _preview, null!));
    }

    /// <summary>
    /// Assert that pressing the mouse pointer at position (X, Y) initiates the square correctly
    /// </summary>
    [Fact]
    public void PointerPressed_InitializesSquareAndShowsPreview()
    {
        ShapeSquareState state = new(_client, _preview, _toolbar);
        Point2D start = new(50, 60);

        state.PointerPressed(start);

        Assert.Single(_preview.ShownItems);
        ISquare square = Assert.IsAssignableFrom<ISquare>(_preview.LastShownItem);
        Assert.Equal(start.X, square.Center.X);
        Assert.Equal(start.Y, square.Center.Y);
        Assert.Equal(0, square.SideLegth);
        Assert.Equal(_toolbar.BorderColor, square.BorderColor);
        Assert.Equal(_toolbar.BorderWidth, square.BorderWidth);
        Assert.Equal(_toolbar.FillColor, square.FillColor);
        Assert.Equal(_client.UserId, square.CreatedBy);
        Assert.Equal(0, square.Zindex);
    }

    /// <summary>
    /// Assert that the Z index is correctly instantiated when items are present on the whiteboard
    /// </summary>
    [Fact]
    public void PointerPressed_AssignsTopZIndexWhenItemsExist()
    {
        _client.Canvas.AddItem(new Square { Zindex = 10 });
        _client.Canvas.AddItem(new Circle { Zindex = 25 });

        ShapeSquareState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(0, 0));

        ISquare square = Assert.IsAssignableFrom<ISquare>(_preview.LastShownItem);
        Assert.Equal(26, square.Zindex);
    }

    /// <summary>
    /// Assert that the circle is setup correctly regardless of where the pointer is with respect to the starting positin 
    /// </summary>
    [Theory]
    [InlineData(10, 10, 30, 30, 20, 20, 20)] // Bottom Right
    [InlineData(50, 10, 30, 30, 20, 40, 20)] // Bottom Left
    [InlineData(10, 50, 30, 30, 20, 20, 40)] // Top Right
    [InlineData(50, 50, 30, 30, 20, 40, 40)] // Top Left
    public void PointerMoved_ComputesCorrectQuadrantCenterAndSide(
        double startX, double startY,
        double moveX, double moveY,
        double expectedSide, double expectedCenterX, double expectedCenterY
    )
    {
        ShapeSquareState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(startX, startY));
        state.PointerMoved(new Point2D(moveX, moveY));

        Assert.Equal(2, _preview.ShownItems.Count);
        ISquare square = Assert.IsAssignableFrom<ISquare>(_preview.LastShownItem);
        Assert.Equal(expectedSide, square.SideLegth);
        Assert.Equal(expectedCenterX, square.Center.X);
        Assert.Equal(expectedCenterY, square.Center.Y);
    }

    /// <summary>
    /// Assert that moving the pointer when drawing does nothing
    /// </summary>
    [Fact]
    public void PointerMoved_WhenNotDrawing_DoesNothing()
    {
        ShapeSquareState state = new(_client, _preview, _toolbar);
        state.PointerMoved(new Point2D(20, 20));

        Assert.Empty(_preview.ShownItems);
    }

    /// <summary>
    /// Ensure the core functionality of square, sending to client and clearing preview works
    /// </summary>
    [Fact]
    public void PointerReleased_WithPositiveSide_SubmitsAndClearsPreview()
    {
        ShapeSquareState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));
        state.PointerMoved(new Point2D(30, 30));

        state.PointerReleased(new Point2D(30, 30));

        Assert.Equal(1, _preview.ClearCount);

        bool wasAdded = _client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.True(wasAdded, "Item was not added in time.");

        Assert.Single(_client.AddedItems);
        ISquare submitted = Assert.IsAssignableFrom<ISquare>(_client.AddedItems[0]);
        Assert.Equal(20, submitted.SideLegth);
    }

    /// <summary>
    /// Assert that clicking a zero sidelength square is not submitted
    /// </summary>
    [Fact]
    public void PointerReleased_WithZeroSide_DoesNotSubmit()
    {
        ShapeSquareState state = new(_client, _preview, _toolbar);
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
        ShapeSquareState state = new(_client, _preview, _toolbar);
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
        ShapeSquareState state = new(_client, _preview, _toolbar);
        state.PointerPressed(new Point2D(10, 10));
        state.PointerMoved(new Point2D(30, 30));

        state.PointerCaptureLost(new Point2D(30, 30));

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);

        // Subsequent move does nothing
        state.PointerMoved(new Point2D(50, 50));
        Assert.Equal(2, _preview.ShownItems.Count);
    }

    /// <summary>
    /// Assert that losing pointer when not drawing does not do anything
    /// </summary>
    [Fact]
    public void PointerCaptureLost_WhenNotDrawing_DoesNothing()
    {
        ShapeSquareState state = new(_client, _preview, _toolbar);
        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Equal(0, _preview.ClearCount);
    }
}
