//owner : U Sai Vignesh
//roll number : 112301034
// License : MIT

using System;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Selection;
using Whiteboard.Services;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the select tool is chosen.
/// Clicking an item selects it, clicking empty canvas deselects,
/// and dragging a selected item moves it.
/// </summary>
public sealed class SelectState : ViewModelStateBase
{
    /// <summary>
    /// Which items are currently selected.
    /// </summary>
    private readonly ISelectionState _selection = new SelectionState();

    /// <summary>
    /// Hit-tests the canvas and updates <see cref="_selection"/>.
    /// </summary>
    private readonly ISelectionService _selectionService;

    /// <summary>
    /// Pointer position where the current drag started.
    /// </summary>
    private Point2D _dragStart = new(0, 0);

    /// <summary>
    /// Copy of the dragged item as it was when the drag started.
    /// Used to compute the target position and to restore it if the drag is interrupted.
    /// </summary>
    private IItem? _originalItem;

    /// <summary>
    /// Whether the item actually moved during the current drag (a plain click does not).
    /// </summary>
    private bool _hasMoved;
    private bool _isResizing;
    private IItem? _resizeOriginalItem;

    public SelectState(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo)
        : base(whiteboardClient, preview, toolbarInfo)
    {
        _selectionService = new SelectionService(WhiteboardClient.Canvas, _selection);

        // Drop the selection if the selected item is erased (by anyone)
        WhiteboardClient.Canvas.ItemDeleted += OnItemDeleted;
    }

    /// <summary>
    /// Raised whenever the selected item changes or the selection is cleared.
    /// May be raised off the UI thread (e.g. when a remote user erases the item).
    /// </summary>
    public event Action? SelectionChanged;

    /// <summary>
    /// Id of the selected item, or null when nothing is selected.
    /// </summary>
    public Guid? SelectedItemId => _selection.PrimarySelectedId;

    /// <summary>
    /// The selected item from the canvas, or null when nothing is selected.
    /// </summary>
    public IItem? SelectedItem =>
        SelectedItemId is Guid id && WhiteboardClient.Canvas.Items.TryGetValue(id, out IItem? item)
            ? item
            : null;

    public bool BeginResize()
    {
        if (SelectedItem is not IItem selected)
        {
            return false;
        }

        _isResizing = true;
        _resizeOriginalItem = ItemSnapshot.Copy(selected);
        return true;
    }

    public void Resize(Point2D point)
    {
        if (!_isResizing || _resizeOriginalItem is null)
        {
            return;
        }

        _selectionService.ResizeSelected(_resizeOriginalItem, point, WhiteboardClient.UserId);
    }

    public void EndResize(bool commit)
    {
        if (!_isResizing)
        {
            return;
        }

        if (commit && SelectedItem is IItem resized)
        {
            WhiteboardClient.EditItem(resized);
        }
        else if (_resizeOriginalItem is not null)
        {
            WhiteboardClient.Canvas.EditItem(_resizeOriginalItem);
        }

        _isResizing = false;
        _resizeOriginalItem = null;
    }

    public override void PointerPressed(Point2D point)
    {
        Guid? previous = SelectedItemId;

        // Selects the topmost item under the pointer, or clears the selection on empty canvas
        _selectionService.SelectAtPoint(point);

        if (previous != SelectedItemId)
        {
            SelectionChanged?.Invoke();
        }

        // Pressing on an item also starts dragging it
        IItem? selected = SelectedItem;
        if (selected is null)
        {
            return;
        }

        _isDrawing = true;
        _dragStart = point;
        _originalItem = ItemSnapshot.Copy(selected);
        _hasMoved = false;
    }

    public override void PointerMoved(Point2D point)
    {
        if (!_isDrawing || _originalItem is null || SelectedItem is not IItem current)
        {
            return;
        }

        // Where the item should be: its start position plus the total pointer offset.
        // Comparing against the current position (not the last pointer point) keeps the
        // item under the pointer even if a server update moved it back mid-drag.
        double targetLeft = _originalItem.BoundingBox.Left + (point.X - _dragStart.X);
        double targetTop = _originalItem.BoundingBox.Top + (point.Y - _dragStart.Y);
        double dx = targetLeft - current.BoundingBox.Left;
        double dy = targetTop - current.BoundingBox.Top;

        if (dx == 0 && dy == 0)
        {
            return;
        }

        // Local only; the final position is sent once on release
        if (_selectionService.MoveSelected(dx, dy))
        {
            _hasMoved = true;
        }
    }

    public override void PointerReleased(Point2D point)
    {
        if (!_isDrawing)
        {
            return;
        }

        bool hasMoved = _hasMoved;
        EndDrag();

        // A plain click (no movement) only selects; nothing to send
        if (hasMoved && SelectedItem is IItem moved)
        {
            WhiteboardClient.EditItem(moved);
        }
    }

    public override void PointerCaptureLost(Point2D point)
    {
        if (!_isDrawing)
        {
            return;
        }

        IItem? original = _originalItem;
        bool hasMoved = _hasMoved;
        EndDrag();

        // Drag was interrupted: put the item back where it started (local only, nothing was sent)
        if (hasMoved && original is not null)
        {
            WhiteboardClient.Canvas.EditItem(original);
        }
    }

    /// <summary>
    /// Resets all drag fields.
    /// </summary>
    private void EndDrag()
    {
        _isDrawing = false;
        _originalItem = null;
        _hasMoved = false;
    }

    /// <summary>
    /// Deselects everything. Does nothing if nothing is selected.
    /// </summary>
    public void ClearSelection()
    {
        if (SelectedItemId is null)
        {
            return;
        }

        _selection.Clear();
        SelectionChanged?.Invoke();
    }

    private void OnItemDeleted(Guid id)
    {
        if (_selection.IsSelected(id))
        {
            ClearSelection();
        }
    }
}
