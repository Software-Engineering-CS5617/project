/// Owner   : Raagam Hitesh Parmar, Sasi Kiran, Sangam,    Karthik Krishna M, Vignesh
/// Roll No : 112301024,            112301033,  112201006, 112301014,         112301034
/// LICENSE : MIT

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Dispatching;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Whiteboard.Client;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Services;
using Whiteboard.Interfaces;
using Whiteboard.Views;

namespace Whiteboard.ViewModels;

public enum WhiteboardTool
{
    Pen,
    Square,
    Rectangle,
    Circle,
    Ellipse,
    EqTriangle,
    Textbox,
    Eraser,
    Line,
    Polygon,
    Select
}

[ExcludeFromCodeCoverage]
// Temporary exclusion from code coverage
public partial class WhiteBoardViewModel : IDisposable
{
    private IViewModelState _penState;
    private IViewModelState _squareState;
    private IViewModelState _rectangleState;
    private IViewModelState _circleState;
    private IViewModelState _ellipseState;
    private IViewModelState _eqTriangleState;
    private TextboxState _textboxState;
    private IViewModelState _eraserState;
    private IViewModelState _lineState;
    private IViewModelState _polygonState;
    private SelectState _selectState;
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly WhiteboardSession? _session;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly IShapeRecognitionService _shapeRecognitionService;
    private WhiteboardRenderer _renderer;

    private IViewModelState _currentState;
    private bool _isInitialized;

    public WhiteboardTool SelectedTool { get; private set; } = WhiteboardTool.Pen;

    private readonly string _sessionId;
    private WhiteboardSnapshotStore? _snapshotStore;

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

    /// <summary>
    /// Gets or sets whether automatic shape recognition is enabled for strokes drawn with the Pen tool.
    /// </summary>
    public bool IsSmartShapeEnabled
    {
        get => _shapeRecognitionService.IsEnabled;
        set => _shapeRecognitionService.IsEnabled = value;
    }

    /// <summary>
    /// Gets the shape recognition service associated with this view model.
    /// </summary>
    public IShapeRecognitionService ShapeRecognitionService => _shapeRecognitionService;

    public WhiteBoardViewModel(IWhiteboardClient whiteboardClient, string sessionId, IShapeRecognitionService? shapeRecognitionService = null)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        _whiteboardClient = whiteboardClient;
        _sessionId = sessionId;
        _shapeRecognitionService = shapeRecognitionService ?? new ShapeRecognitionService();
    }

    public WhiteBoardViewModel(WhiteboardSession session, DispatcherQueue? dispatcherQueue = null, IShapeRecognitionService? shapeRecognitionService = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _dispatcherQueue = dispatcherQueue;
        _whiteboardClient = session.Client;
        _shapeRecognitionService = shapeRecognitionService ?? new ShapeRecognitionService();

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
        _penState = new PenState(_whiteboardClient, preview, toolbarInfo, _shapeRecognitionService);
        _squareState = new ShapeSquareState(_whiteboardClient, preview, toolbarInfo);
        _rectangleState = new ShapeRectangleState(_whiteboardClient, preview, toolbarInfo);
        _circleState = new ShapeCircleState(_whiteboardClient, preview, toolbarInfo);
        _ellipseState = new ShapeEllipseState(_whiteboardClient, preview, toolbarInfo);
        _eraserState = new EraserState(_whiteboardClient);
        _eqTriangleState = new ShapeEqTriangleState(_whiteboardClient, preview, toolbarInfo);
        _textboxState = new TextboxState(_whiteboardClient, preview, toolbarInfo);
        _lineState = new LineState(_whiteboardClient, preview, toolbarInfo);
        _polygonState = new PolygonState(_whiteboardClient, preview, toolbarInfo);
        _selectState = new SelectState(_whiteboardClient, preview, toolbarInfo);
        _renderer = new WhiteboardRenderer(_whiteboardClient.Canvas, canvas, _textboxState, _selectState);
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
            WhiteboardTool.Rectangle => _rectangleState,
            WhiteboardTool.Circle => _circleState,
            WhiteboardTool.Ellipse => _ellipseState,
            WhiteboardTool.EqTriangle => _eqTriangleState,
            WhiteboardTool.Textbox => _textboxState,
            WhiteboardTool.Eraser => _eraserState,
            WhiteboardTool.Line => _lineState,
            WhiteboardTool.Polygon => _polygonState,
            WhiteboardTool.Select => _selectState,
            _ => throw new ArgumentOutOfRangeException(nameof(tool))
        };

        // Leaving the select tool deselects the item
        if (tool != WhiteboardTool.Select)
        {
            _selectState.ClearSelection();
        }

        if (!ReferenceEquals(_currentState, selectedState))
        {
            _currentState.Reset();
            _currentState = selectedState;
            SelectedTool = tool;
        }
    }

    /// <summary>
    /// Resets the currently active drawing state and clears any live preview.
    /// </summary>
    public void ResetCurrentState()
    {
        EnsureInitialized();
        _currentState.Reset();
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

    public void PointerDoubleTapped(Point2D point)
    {
        EnsureInitialized();
        _currentState.PointerDoubleTapped(point);
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

    private async Task<WhiteboardSnapshotStore> GetSnapshotStoreAsync()
    {
        _snapshotStore ??= await WhiteboardSnapshotStore.CreateAsync();
        return _snapshotStore;
    }

    public async Task SaveSnapshotAsync()
    {
        List<ItemDto> items = _whiteboardClient.Canvas.Items.Values.Select(ItemMapper.ToDto).ToList();

        WhiteboardSnapshotStore store = await GetSnapshotStoreAsync();

        await store.SaveSnapshotAsync( _sessionId, _whiteboardClient.UserId, items);
    }

    public async Task<List<WhiteboardSnapshotSummary>> ListSnapshotsAsync()
    {
        WhiteboardSnapshotStore store = await GetSnapshotStoreAsync();
        return await store.ListAsync(_sessionId);
    }

    public async Task RestoreSnapshotAsync(string snapshotId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);

        WhiteboardSnapshotStore store = await GetSnapshotStoreAsync();
        WhiteboardSnapshot snapshot = await store.LoadAsync(_sessionId, snapshotId);

        _whiteboardClient.RestoreSnapshot(new WhiteboardDto(snapshot.Items));
    }
}
