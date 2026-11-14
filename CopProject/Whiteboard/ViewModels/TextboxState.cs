using System;
using System.Collections.Generic;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the textbox tool is selected.
/// Also tracks uncommitted text typed into textboxes so it can be
/// saved when the user switches tools or interacts with the canvas.
/// </summary>
public sealed class TextboxState : ViewModelStateBase
{
    /// <summary>
    /// Latest uncommitted text per textbox item.
    /// </summary>
    private readonly Dictionary<Guid, string> _pendingEdits = [];

    public TextboxState(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo)
        : base(whiteboardClient, preview, toolbarInfo)
    {
    }

    /// <summary>
    /// Presses landing on a textbox are left to the textbox so the user can type into it.
    /// </summary>
    public override bool HandlesPointerOnTextbox => false;

    /// <summary>
    /// Gets whether any textbox has text that has not yet been sent to the client.
    /// </summary>
    public bool HasPendingEdits => _pendingEdits.Count > 0;

    public override void PointerPressed(Point2D point)
    {
        // Save whatever was typed before creating another textbox
        CommitPendingEdits();

        // Reuse an existing textbox instead of stacking a new one on top of it
        if (WhiteboardClient.Canvas.HitTestTopmost(point) is ITextbox)
        {
            return;
        }

        Textbox textbox = new() {
            Center = point,
            CreatedBy = WhiteboardClient.UserId,
            Zindex = GetNextZIndex()
        };

        WhiteboardClient.AddItem(textbox);
    }

    public override void PointerMoved(Point2D point)
    {
    }

    public override void PointerReleased(Point2D point)
    {
    }

    public override void PointerCaptureLost(Point2D point)
    {
    }

    /// <summary>
    /// Records the latest text typed into a textbox. Nothing is sent until
    /// <see cref="CommitPendingEdits"/> is called.
    /// </summary>
    public void UpdateText(Guid itemId, string text)
    {
        text ??= string.Empty;

        // Text identical to the stored content is not an edit
        if (WhiteboardClient.Canvas.Items.TryGetValue(itemId, out IItem? item)
            && item is ITextbox textbox
            && textbox.Content == text)
        {
            _pendingEdits.Remove(itemId);
            return;
        }

        _pendingEdits[itemId] = text;
    }

    /// <summary>
    /// Sends every pending text edit through the whiteboard client.
    /// Edits for items that no longer exist (e.g. erased) are discarded.
    /// </summary>
    public void CommitPendingEdits()
    {
        if (_pendingEdits.Count == 0)
        {
            return;
        }

        // Copy and clear first: EditItem re-renders the textbox, which may raise
        // LostFocus and call back into this method.
        KeyValuePair<Guid, string>[] edits = [.. _pendingEdits];
        _pendingEdits.Clear();

        foreach ((Guid itemId, string text) in edits)
        {
            if (!WhiteboardClient.Canvas.Items.TryGetValue(itemId, out IItem? item)
                || item is not ITextbox textbox
                || textbox.Content == text)
            {
                continue;
            }

            textbox.Content = text;
            WhiteboardClient.EditItem(textbox);
        }
    }
}
