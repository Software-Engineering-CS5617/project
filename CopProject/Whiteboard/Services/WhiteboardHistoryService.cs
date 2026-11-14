// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/****
    * Filename  : WhiteboardHistoryService.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Whiteboard.Interfaces;

namespace Whiteboard.Services;

/// <summary>
/// Stores and applies undo and redo actions for one local user.
/// </summary>
public class WhiteboardHistoryService : IWhiteboardHistory
{
    private readonly IWhiteboard _whiteboard;
    private readonly string _userId;

    // Each stack stores the next actions to execute.
    private readonly Stack<IWhiteboardAction> _undo = new();
    private readonly Stack<IWhiteboardAction> _redo = new();

    private bool _isChangingHistory;

    /// <inheritdoc/>
    public bool CanUndo => _undo.Count > 0;

    /// <inheritdoc/>
    public bool CanRedo => _redo.Count > 0;

    /// <inheritdoc/>
    public event Action HistoryChanged = delegate { };

    /// <summary>
    /// Creates an empty history for the given board and user.
    /// </summary>
    /// <param name="whiteboard">Board that actions will modify.</param>
    /// <param name="userId">User who owns this history.</param>
    public WhiteboardHistoryService(
        IWhiteboard whiteboard,
        string userId)
    {
        ArgumentNullException.ThrowIfNull(whiteboard);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        _whiteboard = whiteboard;
        _userId = userId;
        Trace.TraceInformation($"[WhiteboardHistoryService] History created. User={_userId}, Undo=0, Redo=0");
    }

    /// <summary>
    /// Performs a new action and saves its inverse for undo.
    /// </summary>
    /// <param name="action">Action to perform.</param>
    public void Execute(IWhiteboardAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        ChangeHistory("Execute", () => {
            ValidateUser(action);

            // Update history only after the action and its inverse succeed.
            action.Perform(_whiteboard);
            IWhiteboardAction inverse = GetInverse(action);

            _undo.Push(inverse);

            // A new action replaces the previous redo path.
            int clearedRedo = _redo.Count;
            _redo.Clear();
            Trace.TraceInformation($"[WhiteboardHistoryService] Action recorded. User={_userId}, Action={action.ActionId}, ClearedRedo={clearedRedo}");
        });
    }

    /// <summary>
    /// Applies the next undo action and saves its inverse for redo.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo)
        {
            Trace.TraceInformation($"[WhiteboardHistoryService] Undo skipped: history is empty. User={_userId}");
            return;
        }

        ChangeHistory("Undo", () => Transfer(_undo, _redo));
    }

    /// <summary>
    /// Applies the next redo action and saves its inverse for undo.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo)
        {
            Trace.TraceInformation($"[WhiteboardHistoryService] Redo skipped: history is empty. User={_userId}");
            return;
        }

        ChangeHistory("Redo", () => Transfer(_redo, _undo));
    }

    /// <summary>
    /// Executes an action from one stack and stores its inverse in the other.
    /// </summary>
    /// <param name="source">Stack containing the action.</param>
    /// <param name="destination">Stack receiving the inverse.</param>
    private void Transfer(
        Stack<IWhiteboardAction> source,
        Stack<IWhiteboardAction> destination)
    {
        // Keep the entry in place if execution fails.
        IWhiteboardAction action = source.Peek();
        ValidateUser(action);

        action.Perform(_whiteboard);
        IWhiteboardAction inverse = GetInverse(action);

        source.Pop();
        destination.Push(inverse);
    }

    /// <summary>
    /// Creates and validates an action's inverse.
    /// </summary>
    /// <param name="action">Action to reverse.</param>
    /// <returns>An inverse belonging to the same user.</returns>
    private IWhiteboardAction GetInverse(IWhiteboardAction action)
    {
        IWhiteboardAction inverse = action.CreateInverse();

#pragma warning disable IDE0270 // Use coalesce expression
        if (inverse is null)
        {
            throw new InvalidOperationException(
                "A history action must provide an inverse.");
        }
#pragma warning restore IDE0270 // Use coalesce expression

        ValidateUser(inverse);
        return inverse;
    }

    /// <summary>
    /// Checks that the action belongs to this history's user.
    /// </summary>
    /// <param name="action">Action to check.</param>
    private void ValidateUser(IWhiteboardAction action)
    {
        if (!string.Equals(action.UserId, _userId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "This history belongs to a different user.");
        }
    }

    /// <summary>
    /// Runs a history operation and notifies listeners when it succeeds.
    /// </summary>
    /// <param name="operation">History operation to run.</param>
    private void ChangeHistory(string operationName, Action operation)
    {
        if (_isChangingHistory)
        {
            Trace.TraceError($"[WhiteboardHistoryService][Error] {operationName} rejected: another history operation is running. User={_userId}");
            throw new InvalidOperationException(
                "Another history operation is already running.");
        }

        _isChangingHistory = true;

        try
        {
            operation();
        }
        catch (Exception exception)
        {
            Trace.TraceError($"[WhiteboardHistoryService][Error] {operationName} failed. User={_userId}, Undo={_undo.Count}, Redo={_redo.Count}. {exception}");
            throw;
        }
        finally
        {
            // Allow future operations even if this one fails.
            _isChangingHistory = false;
        }

        Trace.TraceInformation($"[WhiteboardHistoryService] {operationName} completed. User={_userId}, Undo={_undo.Count}, Redo={_redo.Count}");
        HistoryChanged();
    }
}
