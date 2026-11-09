// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Network.Messages;

/// <summary>
/// Message sent from a client to the server
/// </summary 
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ReqUpdate), "req_update")]
[JsonDerivedType(typeof(ReqAddItem), "req_add_item")]
[JsonDerivedType(typeof(ReqDeleteItem), "req_delete_item")]
[JsonDerivedType(typeof(ReqUpdateItem), "req_update_item")]
[JsonDerivedType(typeof(ReqUndo), "req_undo")]
[JsonDerivedType(typeof(ReqRedo), "req_redo")]
[ExcludeFromCodeCoverage]
public abstract record ClientMessage
{
    private ClientMessage() { }

    /// <summary>
    /// Identifies the user performing this operation.
    /// This may differ from the creator of the affected item.
    /// </summary>
    // Set by WhiteboardClient.SendToServer for every request.
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Sent when a new client connects the whiteboard,
    /// Or when the client believes its state is out of sync from the server.
    /// TODO How does client know that?
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed record ReqUpdate() : ClientMessage;

    /// <summary>
    /// Sent when a client adds an item to its state,
    /// and requests the server to approve its addition.
    /// </summary>
    /// <param name="Id">GUID of the added item</param>
    /// <param name="Item">Added item</param>
    [ExcludeFromCodeCoverage]
    public sealed record ReqAddItem(Guid Id, ItemDto Item) : ClientMessage;

    /// <summary>
    /// Sent when a client deletes an item from its state,
    /// and requests the server to approve the deletion.
    /// </summary>
    /// <param name="Id">GUID of the deleted item</param>
    [ExcludeFromCodeCoverage]
    public sealed record ReqDeleteItem(Guid Id) : ClientMessage;

    /// <summary>
    /// Sent when a client edits an item from its state,
    /// and requests the server to approve the edit.
    /// </summary>
    /// <param name="Id">GUID of the updated item</param>
    /// <param name="Item">Updated item</param>
    [ExcludeFromCodeCoverage]
    public sealed record ReqUpdateItem(Guid Id, ItemDto Item) : ClientMessage;

    /// <summary>
    /// Requests undo of the requesting user's most recent
    /// available action.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed record ReqUndo() : ClientMessage;

    /// <summary>
    /// Requests redo of the requesting user's most recently
    /// undone action that remains available.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed record ReqRedo() : ClientMessage;
}
