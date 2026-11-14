/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Views;

namespace Whiteboard.ViewModels;

public enum WhiteboardTool
{
    Pen,
    Square,
    Circle,
    EqTriangle,
    Textbox,
    Eraser
}

[ExcludeFromCodeCoverage]
// Temporary exclusion from code coverage
public class WhiteBoardViewModel
{
    private IViewModelState _penState;
    private IViewModelState _squareState;
    private IViewModelState _circleState;
    private IViewModelState _eqTriangleState;
    private TextboxState _textboxState;
    private IViewModelState _eraserState;
    private readonly IWhiteboardClient _whiteboardClient;
    private WhiteboardRenderer _renderer;

    private IViewModelState _currentState;
    private bool _isInitialized;

    public WhiteboardTool SelectedTool { get; private set; } = WhiteboardTool.Pen;

    /// <summary>
    /// Whether a pointer press that lands on a rendered textbox should be forwarded
    /// to the current tool (e.g. eraser) instead of being left to the textbox.
    /// </summary>
    public bool HandlesPointerOnTextbox
    {
        get
        {
            EnsureInitialized();
            return _currentState.HandlesPointerOnTextbox;
        }
    }


    /// <summary>
    /// Gets undo availability last received for this user.
    /// </summary>
    public bool CanUndo => _whiteboardClient.CanUndo;

    /// <summary>
    /// Gets redo availability last received for this user.
    /// </summary>
    public bool CanRedo => _whiteboardClient.CanRedo;

    /// <summary>
    /// Forwards this user's server history notifications to the view.
    /// </summary>
    public event Action HistoryChanged
    {
        add => _whiteboardClient.HistoryChanged += value;
        remove => _whiteboardClient.HistoryChanged -= value;
    }

    public WhiteBoardViewModel(IWhiteboardClient whiteboardClient)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);

        _whiteboardClient = whiteboardClient;
    }

    public void InitializeViewModel(Canvas canvas, ToolbarInfo toolbarInfo)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("The whiteboard view model has already been initialized.");
        }

        CanvasPreview preview = new(canvas);
        _penState = new PenState(_whiteboardClient, preview, toolbarInfo);
        _squareState = new ShapeSquareState(_whiteboardClient, preview, toolbarInfo);
        _circleState = new ShapeCircleState(_whiteboardClient, preview, toolbarInfo);
        _eraserState = new EraserState(_whiteboardClient);
        _eqTriangleState = new ShapeEqTriangleState(_whiteboardClient, preview, toolbarInfo);
        _textboxState = new TextboxState(_whiteboardClient, preview, toolbarInfo);
        _renderer = new WhiteboardRenderer(_whiteboardClient.Canvas, canvas, _textboxState);

        _currentState = _penState;
        _isInitialized = true;
    }

    /// <summary>
    /// Requests undo for the current user through the shared client.
    /// </summary>
    public void Undo()
    {
        _whiteboardClient.Undo();
    }

    /// <summary>
    /// Requests redo for the current user through the shared client.
    /// </summary>
    public void Redo()
    {
        _whiteboardClient.Redo();
    }

    /// <summary>
    /// Switches the active state
    /// </summary>
    public void SelectTool(WhiteboardTool tool)
    {
        EnsureInitialized();

        // Save any text typed into a textbox before leaving the current tool
        _textboxState.CommitPendingEdits();

        IViewModelState selectedState = tool switch {
            WhiteboardTool.Pen => _penState,
            WhiteboardTool.Square => _squareState,
            WhiteboardTool.Circle => _circleState,
            WhiteboardTool.EqTriangle => _eqTriangleState,
            WhiteboardTool.Textbox => _textboxState,
            WhiteboardTool.Eraser => _eraserState,
            _ => throw new ArgumentOutOfRangeException(nameof(tool))
        };

        _currentState = selectedState;
        SelectedTool = tool;
    }

    public void PointerPressed(Point2D point)
    {
        EnsureInitialized();

        // Interacting with the canvas ends any in-progress text edit
        _textboxState.CommitPendingEdits();
        _currentState.PointerPressed(point);
    }

    public void PointerMoved(Point2D point)
    {
        EnsureInitialized();
        _currentState.PointerMoved(point);
    }

    public void PointerReleased(Point2D point)
    {
        EnsureInitialized();
        _currentState.PointerReleased(point);
    }

    public void PointerCaptureLost(Point2D point)
    {
        EnsureInitialized();
        _currentState.PointerCaptureLost(point);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("The whiteboard view model has not been initialized.");
        }
    }
}
