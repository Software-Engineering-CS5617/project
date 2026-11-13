/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
    Circle,
    EqTriangle,
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
    private IViewModelState _eraserState;
    private readonly IWhiteboardClient _whiteboardClient;
    private WhiteboardRenderer _renderer;

    private IViewModelState _currentState;
    private bool _isInitialized;

    public WhiteboardTool SelectedTool { get; private set; } = WhiteboardTool.Pen;

    private readonly string _sessionId;
    private WhiteboardSnapshotStore? _snapshotStore;


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

    public WhiteBoardViewModel(IWhiteboardClient whiteboardClient, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        _whiteboardClient = whiteboardClient;
        _sessionId = sessionId;
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
