// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Networking;
using Whiteboard.Interfaces;

namespace Whiteboard.Client;

/// <summary>
/// Whiteboard client state manager
/// </summary>
public interface IWhiteboardClient : IMessageListener
{
    /// <summary>
    /// Client whiteboard state
    /// </summary>
    public IWhiteboard Canvas { get; }

    /// <summary>
    /// Username of the current client
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Gets whether the server reports an available undo action for this user.
    /// </summary>
    public bool CanUndo { get; }

    /// <summary>
    /// Gets whether the server reports an available redo action for this user.
    /// </summary>
    public bool CanRedo { get; }

    /// <summary>
    /// Raised when this user's history availability arrives from the server.
    /// UI subscribers must dispatch control updates to the UI thread.
    /// </summary>
    event Action HistoryChanged;

    /// <summary>
    /// Request a complete whiteboard sync to the server
    /// </summary>
    public void RequestSync();

    /// <summary>
    /// Request to add an <see cref="IItem"/> to the server
    /// </summary>
    /// <param name="item">Item to add</param>
    public void AddItem(IItem item);

    /// <summary>
    /// Request to delete an <see cref="IItem"/> to the server
    /// </summary>
    /// <param name="id">Item Id to delete</param>
    public void DeleteItem(Guid id);

    /// <summary>
    /// Request to edit an <see cref="IITem"/> to the server
    /// </summary>
    /// <param name="item">Item to edit</param>
    public void EditItem(IItem item);

    /// <summary>
    /// Requests undo of this user's latest available action.
    /// An empty history leaves the canvas unchanged.
    /// </summary>
    public void Undo();

    /// <summary>
    /// Requests redo of this user's latest available undone action.
    /// An empty redo history leaves the canvas unchanged.
    /// </summary>
    public void Redo();
}
