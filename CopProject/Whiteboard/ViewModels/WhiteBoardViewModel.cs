/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Whiteboard.Client;

namespace Whiteboard.ViewModels;

public enum WhiteboardTool
{
    Pen,
    Square,
    Eraser
}

[ExcludeFromCodeCoverage]
// Temporary exclusion from code coverage
public class WhiteBoardViewModel
{
    private IViewModelState _penState;
    private IViewModelState _squareState;
    private IViewModelState _eraserState;
    private readonly IWhiteboardClient _whiteboardClient;
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

    public void InitializeViewModel(Canvas canvas, ToolbarInfo toolbarInfo)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("The whiteboard view model has already been initialized.");
        }

        _penState = new PenState(_whiteboardClient, canvas, toolbarInfo);
        _squareState = new ShapeSquareState(_whiteboardClient, canvas, toolbarInfo);
        _eraserState = new EraserState(_whiteboardClient, canvas);
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
            WhiteboardTool.Eraser => _eraserState,
            _ => throw new ArgumentOutOfRangeException(nameof(tool))
        };

        _currentState = selectedState;
        SelectedTool = tool;
    }

    public void PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        EnsureInitialized();
        _currentState.PointerPressed(sender, e);
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        EnsureInitialized();
        _currentState.PointerMoved(sender, e);
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EnsureInitialized();
        _currentState.PointerReleased(sender, e);
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        EnsureInitialized();
        _currentState.PointerCaptureLost(sender, e);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("The whiteboard view model has not been initialized.");
        }
    }
}
