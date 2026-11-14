/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

/// <summary>
/// Test state for view model
/// </summary>
/// <inheritdoc/>
internal class TestViewModelState : ViewModelStateBase
{
    public TestViewModelState(IWhiteboardClient client, ICanvasPreview preview, ToolbarInfo toolbar) : base(client, preview, toolbar)
    {
    }

    public double CallGetNextZIndex()
    {
        return GetNextZIndex();
    }

    public override void PointerPressed(Point2D point) { }
    public override void PointerMoved(Point2D point) { }
    public override void PointerReleased(Point2D point) { }
    public override void PointerCaptureLost(Point2D point) { }
}

/// <summary>
/// Tests for the test view model state
/// </summary>
public class ViewModelStateBaseTests
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
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 0, 0, 0), 1.0, Color.FromArgb(0, 0, 0, 0));

    /// <summary>
    /// Assert that constructor throws <see cref="ArgumentNullException"/> if any of the arguments is null
    /// </summary>
    [Fact]
    public void Constructor_NullValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new TestViewModelState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new TestViewModelState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new TestViewModelState(_client, _preview, null!));
    }

    [Fact]
    public void GetNextZIndex_EmptyCanvas_ReturnsZero()
    {
        TestViewModelState state = new(_client, _preview, _toolbar);
        Assert.Equal(0, state.CallGetNextZIndex());
    }

    [Fact]
    public void GetNextZIndex_WithItems_ReturnsMaxPlusOne()
    {
        _client.Canvas.AddItem(new Square { Zindex = -5 });
        _client.Canvas.AddItem(new Square { Zindex = 100 });
        _client.Canvas.AddItem(new Square { Zindex = 42 });

        TestViewModelState state = new(_client, _preview, _toolbar);
        Assert.Equal(101, state.CallGetNextZIndex());
    }
}
