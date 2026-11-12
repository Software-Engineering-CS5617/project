/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Whiteboard.Client;

namespace Whiteboard.ViewModels;

public enum WhiteboardTool
{
    Pen,
    Square,
    Circle,
    EqTriangle,
    Ellipse,
    Eraser
}

public class WhiteBoardViewModel
{
    private IViewModelState _penState;
    private IViewModelState _squareState;
    private IViewModelState _circleState;
    private IViewModelState _eqTriangleState;
    private IViewModelState _ellipseState;
    private IViewModelState _eraserState;
    private readonly IWhiteboardClient _whiteboardClient;
    private WhiteboardRenderer _renderer;

    private IViewModelState _currentState;

    public WhiteBoardViewModel(IWhiteboardClient whiteboardClient)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);

        _whiteboardClient = whiteboardClient;
    }

    public void InitializeViewModel(Canvas canvas, ToolbarInfo toolbarInfo)
    {
        _penState = new PenState(_whiteboardClient, canvas, toolbarInfo);
        _squareState = new ShapeSquareState(_whiteboardClient, canvas, toolbarInfo);
        _circleState = new ShapeCircleState(_whiteboardClient, canvas, toolbarInfo);
        _eqTriangleState = new ShapeEqTriangleState(_whiteboardClient, canvas, toolbarInfo);
        _ellipseState = new ShapeEllipseState(_whiteboardClient, canvas, toolbarInfo);
        _eraserState = new EraserState(_whiteboardClient, canvas);
        _renderer = new WhiteboardRenderer(_whiteboardClient.Canvas, canvas);

        _currentState = _penState;
    }

    /// <summary>
    /// Switches the active state
    /// </summary>
    public void SelectTool(WhiteboardTool tool)
    {
        _currentState = tool switch {
            WhiteboardTool.Pen => _penState,
            WhiteboardTool.Square => _squareState,
            WhiteboardTool.Circle => _circleState,
            WhiteboardTool.EqTriangle => _eqTriangleState,
            WhiteboardTool.Ellipse => _ellipseState,
            WhiteboardTool.Eraser => _eraserState,
            _ => throw new ArgumentOutOfRangeException(nameof(tool))
        };
    }

    public void PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _currentState.PointerPressed(sender, e);
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        _currentState.PointerMoved(sender, e);
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _currentState.PointerReleased(sender, e);
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _currentState.PointerCaptureLost(sender, e);
    }
}
