/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Services;
using Whiteboard.Views;

namespace Whiteboard.ViewModels;

public enum WhiteboardTool
{
    Pen,
    Square,
    Circle,
    EqTriangle,
    Eraser
}

[ExcludeFromCodeCoverage]
// Temporary exclusion from code coverage
public partial class WhiteBoardViewModel : IDisposable
{
    private IViewModelState _penState;
    private IViewModelState _squareState;
    private IViewModelState _circleState;
    private IViewModelState _eqTriangleState;
    private IViewModelState _eraserState;
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly WhiteboardSession? _session;
    private readonly DispatcherQueue? _dispatcherQueue;
    private WhiteboardRenderer _renderer;

    private IViewModelState _currentState;
    private bool _isInitialized;

    public WhiteboardTool SelectedTool { get; private set; } = WhiteboardTool.Pen;


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

    public WhiteBoardViewModel(WhiteboardSession session, DispatcherQueue? dispatcherQueue = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _dispatcherQueue = dispatcherQueue;
        _whiteboardClient = session.Client;

        _session.UserLeft += OnSessionUserLeft;
    }

    private void OnSessionUserLeft(string username)
    {
        if (_dispatcherQueue is not null)
        {
            _dispatcherQueue.TryEnqueue(() => OnUserLeft(username));
        }
        else
        {
            OnUserLeft(username);
        }
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
        _renderer = new WhiteboardRenderer(_whiteboardClient.Canvas, canvas);

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

        IViewModelState selectedState = tool switch {
            WhiteboardTool.Pen => _penState,
            WhiteboardTool.Square => _squareState,
            WhiteboardTool.Circle => _circleState,
            WhiteboardTool.EqTriangle => _eqTriangleState,
            WhiteboardTool.Eraser => _eraserState,
            _ => throw new ArgumentOutOfRangeException(nameof(tool))
        };

        _currentState = selectedState;
        SelectedTool = tool;
    }

    public void PointerPressed(Point2D point)
    {
        EnsureInitialized();
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

    /// <summary>
    /// Force a pointer lost event for the user who left the meeting
    /// </summary>
    /// <param name="username">Username of the leaving user</param>
    public void OnUserLeft(string username)
    {
        if (_isInitialized && _whiteboardClient.UserId == username)
        {
            _currentState.PointerCaptureLost(new Point2D(0, 0));
        }
    }

    /// <summary>
    /// Handles session termination when the host ends the meeting
    /// </summary>
    public void OnMeetingEnded()
    {
        if (_isInitialized)
        {
            _currentState.PointerCaptureLost(new Point2D(0, 0));
        }
    }

    /// <summary>
    /// Disposes the renderer and event subscriptions.
    /// </summary>
    public void Dispose()
    {
        _session?.UserLeft -= OnSessionUserLeft;

        _renderer?.Dispose();
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("The whiteboard view model has not been initialized.");
        }
    }
}
