// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Whiteboard.Network.Dtos;
using System.Collections.Generic;

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
    /// Supplies the server's current undo and redo history for one user.
    /// Clients keep only the history matching their own user ID.
    /// </summary>
    /// <param name="UserId">User who owns this history.</param>
    /// <param name="Undo">
    /// Available undo entries. The last entry is the next action to undo.
    /// </param>
    /// <param name="Redo">
    /// Available redo entries. The last entry is the next action to redo.
    /// </param>
    [ExcludeFromCodeCoverage]
    public sealed record ResHistory(
        string UserId,
        List<HistoryEntryDto> Undo,
        List<HistoryEntryDto> Redo) : ServerMessage;
}
