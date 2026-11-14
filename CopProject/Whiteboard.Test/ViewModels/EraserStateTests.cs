/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;

namespace Whiteboard.Test.ViewModels;

/// <summary>
/// Eraser state tests
/// </summary>
public class EraserStateTests
{
    /// <summary>
    /// Mock whiteboard client
    /// </summary>
    private readonly MockWhiteboardClient _client = new();

    /// <summary>
    /// Assert that constructor throws <see cref="ArgumentNullException"/> if any of the arguments is null
    /// </summary>
    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EraserState(null!));
    }

    /// <summary>
    /// Assert that pointer pressed when an item is hit, deletes said item
    /// </summary>
    [Fact]
    public void PointerPressed_WhenItemHit_DeletesItem()
    {
        Square square = new() {
            Center = new Point2D(50, 50),
            SideLegth = 40
        };
        _client.Canvas.AddItem(square);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(50, 50));

        Assert.Single(_client.DeletedItemIds);
        Assert.Equal(square.Id, _client.DeletedItemIds[0]);
    }

    /// <summary>
    /// Assert that pointer pressed, when no item is hit, does not delete anything
    /// </summary>
    [Fact]
    public void PointerPressed_WhenNoItemHit_DoesNotDelete()
    {
        Square square = new() {
            Center = new Point2D(50, 50),
            SideLegth = 20
        };
        _client.Canvas.AddItem(square);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(100, 100));

        Assert.Empty(_client.DeletedItemIds);
    }

    /// <summary>
    /// Assert that all other pointer events do nothing
    /// </summary>
    [Fact]
    public void PointerMoved_Released_CaptureLost_DoNothing()
    {
        EraserState state = new(_client);

        state.PointerMoved(new Point2D(10, 10));
        state.PointerReleased(new Point2D(10, 10));
        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Empty(_client.DeletedItemIds);
    }
}
