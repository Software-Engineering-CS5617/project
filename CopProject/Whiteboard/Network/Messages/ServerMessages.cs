// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Network.Messages;

/// <summary>
/// Message sent from the server to one or all clients
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ResUpdate), "res_update")]
[JsonDerivedType(typeof(ResAddItem), "res_add_item")]
[JsonDerivedType(typeof(ResDeleteItem), "res_delete_item")]
[JsonDerivedType(typeof(ResUpdateItem), "res_update_item")]
[JsonDerivedType(typeof(ResHistory), "res_history")]
[ExcludeFromCodeCoverage]
public abstract record ServerMessage
{
    private ServerMessage() { }

    /// <summary>
    /// Sent when a client requested a complete whiteboard update (unicast).
    /// </summary>
    /// <param name="Whiteboard">Entire whiteboard state from the server</param>
    [ExcludeFromCodeCoverage]
    public sealed record ResUpdate(WhiteboardDto Whiteboard) : ServerMessage;

    /// <summary>
    /// Sent when a server adds an item to its state (broadcast).
    /// </summary>
    /// <param name="Id">GUID of the added item</param>
    /// <param name="Item">Item added</param>
    [ExcludeFromCodeCoverage]
    public sealed record ResAddItem(Guid Id, ItemDto Item) : ServerMessage;

    /// <summary>
    /// Sent when a server deletes an item from its state (broadcast).
    /// </summary>
    /// <param name="Id">GUID of the deleted item</param>
    [ExcludeFromCodeCoverage]
    public sealed record ResDeleteItem(Guid Id) : ServerMessage;

    /// <summary>
    /// Sent when a server updates an item in its state (broadcast).
    /// </summary>
    /// <param name="Id">GUID of the updated item</param>
    /// <param name="Item">Updated item</param>
    [ExcludeFromCodeCoverage]
    public sealed record ResUpdateItem(Guid Id, ItemDto Item) : ServerMessage;

    /// <summary>
    /// Reports undo and redo availability for one user.
    /// Clients ignore this notification when it belongs to another user.
    /// </summary>
    /// <param name="UserId">User whose history is described.</param>
    /// <param name="CanUndo">Whether this user can undo an action.</param>
    /// <param name="CanRedo">Whether this user can redo an action.</param>
    [ExcludeFromCodeCoverage]
    public sealed record ResHistory(
        string UserId,
        bool CanUndo,
        bool CanRedo) : ServerMessage;
}
