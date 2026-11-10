// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/****
    * Filename  : WhiteboardHistoryService.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/

#nullable enable

using System;
using System.Collections.Generic;
using Whiteboard.Interfaces;
using Whiteboard.Network.Mappers;
using Whiteboard.Network.Serializations;
using System.Linq;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Services;

/// <summary>
/// Stores separate histories for users sharing one server canvas.
/// The server must serialize calls to this service.
/// </summary>
public sealed class SharedWhiteboardHistoryService
{
    private readonly IWhiteboard _canvas;

    private readonly Dictionary<string, UserHistory> _histories =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Stores independent item snapshots before and after one accepted operation.
    /// </summary>
    /// <param name="ItemId">Identity preserved across undo and redo.</param>
    /// <param name="Before">Prior state; null means the item did not exist.</param>
    /// <param name="After">Resulting state; null means the item was deleted.</param>
    private sealed record Change(
        Guid ItemId,
        IItem? Before,
        IItem? After);

    /// <summary>
    /// Keeps undo and redo entries for one actor. The last entry is the next operation.
    /// </summary>
    private sealed class UserHistory
    {
        /// <summary>
        /// Gets changes available for this user to undo.
        /// </summary>
        public List<Change> Undo { get; } = new();
        /// <summary>
        /// Gets undone changes available for this user to redo.
        /// </summary>
        public List<Change> Redo { get; } = new();
    }

    /// <summary>
    /// Creates empty per-user histories for the shared server canvas.
    /// </summary>
    /// <param name="canvas">Canvas modified by accepted operations.</param>
    public SharedWhiteboardHistoryService(IWhiteboard canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        _canvas = canvas;
    }

    /// <summary>
    /// Creates an empty history for a new user without resetting an existing history.
    /// </summary>
    /// <param name="userId">Identity to register.</param>
    public void RegisterUser(string userId)
    {
        GetHistory(userId);
    }

    /// <summary>
    /// Adds an item and records it in the actor's history.
    /// Duplicate IDs leave the canvas and history unchanged.
    /// </summary>
    /// <param name="userId">User performing the addition.</param>
    /// <param name="item">Item to snapshot and add.</param>
    public void AddItem(string userId, IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_canvas.Items.ContainsKey(item.Id))
        {
            return;
        }

        IItem copy = ItemSnapshot.Copy(item);
        copy.CreatedBy = userId;

        Commit(userId, copy.Id, copy);
    }

    /// <summary>
    /// Deletes an item and saves its state for undo.
    /// Missing items leave the canvas and history unchanged.
    /// </summary>
    /// <param name="userId">User performing the deletion.</param>
    /// <param name="itemId">ID of the item to delete.</param>
    public void DeleteItem(string userId, Guid itemId)
    {
        if (!_canvas.Items.ContainsKey(itemId))
        {
            return;
        }

        Commit(userId, itemId, null);
    }

    /// <summary>
    /// Records an item replacement while preserving the original creator.
    /// Missing items leave the canvas and history unchanged.
    /// </summary>
    /// <param name="userId">User performing the edit.</param>
    /// <param name="item">Replacement item state.</param>
    public void EditItem(string userId, IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!_canvas.Items.TryGetValue(item.Id, out IItem? original))
        {
            return;
        }

        IItem copy = ItemSnapshot.Copy(item);

        // The editor and original creator may be different people.
        copy.CreatedBy = original.CreatedBy;
        copy.CreatedDateTime = original.CreatedDateTime;

        Commit(userId, copy.Id, copy);
    }

    /// <summary>
    /// Undoes this user's latest available operation; an empty history does nothing.
    /// </summary>
    /// <param name="userId">User whose operation should be undone.</param>
    public void Undo(string userId)
    {
        UserHistory history = GetHistory(userId);

        Transfer(
            userId,
            history.Undo,
            history.Redo,
            undo: true);
    }

    /// <summary>
    /// Reapplies this user's latest undone operation; an empty redo history does nothing.
    /// </summary>
    /// <param name="userId">User whose operation should be redone.</param>
    public void Redo(string userId)
    {
        UserHistory history = GetHistory(userId);

        Transfer(
            userId,
            history.Redo,
            history.Undo,
            undo: false);
    }

    /// <summary>
    /// Enumerates undo and redo availability for registered users.
    /// </summary>
    /// <remarks>
    /// Enumerate while holding the same server lock used for mutations.
    /// </remarks>
    /// <returns>One history state per user.</returns>
    public IEnumerable<(string UserId, bool CanUndo, bool CanRedo)>
        GetHistoryStates()
    {
#pragma warning disable IDE0008 // Use explicit type
        foreach (var pair in _histories)
        {
            yield return (
                pair.Key,
                pair.Value.Undo.Count > 0,
                pair.Value.Redo.Count > 0);
        }
#pragma warning restore IDE0008 // Use explicit type
    }

    /// <summary>
    /// Exports each user's undo and redo entries for client-side storage.
    /// </summary>
    /// <remarks>
    /// Enumerate while holding the server lock used for history changes.
    /// </remarks>
    /// <returns>
    /// Each user's history lists, with the next operation at the end.
    /// </returns>
    public IEnumerable<(
        string UserId,
        List<HistoryEntryDto> Undo,
        List<HistoryEntryDto> Redo)> GetHistorySnapshots()
    {
#pragma warning disable IDE0008 // Use explicit type
        foreach (var pair in _histories)
        {
            yield return (
                pair.Key,
                pair.Value.Undo.Select(ToDto).ToList(),
                pair.Value.Redo.Select(ToDto).ToList());
        }
#pragma warning restore IDE0008 // Use explicit type
    }

    /// <summary>
    /// Creates a transferable copy of a saved history entry.
    /// </summary>
    /// <param name="change">History entry to export.</param>
    /// <returns>An entry with independent item snapshots.</returns>
    private static HistoryEntryDto ToDto(Change change)
    {
        // Copy before mapping so mutable point lists are not shared
        // with the server's saved history.
        return new HistoryEntryDto(
            change.ItemId,
            change.Before is null
                ? null
                : ItemMapper.ToDto(ItemSnapshot.Copy(change.Before)),
            change.After is null
                ? null
                : ItemMapper.ToDto(ItemSnapshot.Copy(change.After)));
    }

    /// <summary>
    /// Finds a user's history or creates an empty one.
    /// </summary>
    /// <param name="userId">Nonblank user identity.</param>
    /// <returns>The history belonging to this user.</returns>
    private UserHistory GetHistory(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (!_histories.TryGetValue(userId, out UserHistory? history))
        {
            history = new UserHistory();
            _histories.Add(userId, history);
        }

        return history;
    }

    /// <summary>
    /// Applies a new state, records its snapshots, and clears the actor's redo path.
    /// </summary>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="itemId">ID of the affected item.</param>
    /// <param name="replacement">Replacement state, or null for deletion.</param>
    private void Commit(string userId, Guid itemId, IItem? replacement)
    {
        UserHistory history = GetHistory(userId);

        IItem? before =
            _canvas.Items.TryGetValue(itemId, out IItem? existing)
                ? ItemSnapshot.Copy(existing)
                : null;

        IItem? after = replacement is null
            ? null
            : ItemSnapshot.Copy(replacement);

        // An identical edit is not another undo step.
        if (Serialize(before) == Serialize(after))
        {
            return;
        }

        Apply(itemId, after);

        history.Undo.Add(new Change(itemId, before, after));
        // A new operation replaces only this actor's previous redo path.
        history.Redo.Clear();

        InvalidateOtherHistories(userId, itemId);
    }

    /// <summary>
    /// Applies an undo or redo snapshot and moves the entry between history lists.
    /// </summary>
    /// <param name="userId">User performing undo or redo.</param>
    /// <param name="source">List providing its latest entry.</param>
    /// <param name="destination">List receiving the applied entry.</param>
    /// <param name="undo">True applies the before snapshot; false applies the after snapshot.</param>
    private void Transfer(
        string userId,
        List<Change> source,
        List<Change> destination,
        bool undo)
    {
        if (source.Count == 0)
        {
            return;
        }

        Change change = source[source.Count - 1];

        Apply(
            change.ItemId,
            undo ? change.Before : change.After);

        // Move the entry only after applying its state.
        source.RemoveAt(source.Count - 1);
        destination.Add(change);

        InvalidateOtherHistories(userId, change.ItemId);
    }

    /// <summary>
    /// Restores an independent snapshot, or removes the item when the snapshot is null.
    /// </summary>
    /// <param name="itemId">ID of the affected item.</param>
    /// <param name="snapshot">State to restore, or null when the item should be absent.</param>
    private void Apply(Guid itemId, IItem? snapshot)
    {
        if (snapshot is null)
        {
            _canvas.DeleteItem(itemId);
            return;
        }

        // Live items must not share mutable data with saved history.
        IItem item = ItemSnapshot.Copy(snapshot);

        if (_canvas.Items.ContainsKey(itemId))
        {
            _canvas.EditItem(item);
        }
        else
        {
            _canvas.AddItem(item);
        }
    }

    /// <summary>
    /// Removes other users' stale undo and redo entries for this item.
    /// Entries for unrelated items remain available.
    /// </summary>
    /// <remarks>
    /// Invalidation is permanent. Undoing the later change does not restore these entries.
    /// </remarks>
    /// <param name="userId">Actor responsible for the change.</param>
    /// <param name="itemId">ID whose conflicting history must be discarded.</param>
    private void InvalidateOtherHistories(string userId, Guid itemId)
    {
#pragma warning disable IDE0008 // Use explicit type
        foreach (var pair in _histories)
        {
            if (pair.Key == userId)
            {
                continue;
            }

            // Prevent an older undo/redo from overwriting another
            // user's newer change to this same item.
            pair.Value.Undo.RemoveAll(
                change => change.ItemId == itemId);

            pair.Value.Redo.RemoveAll(
                change => change.ItemId == itemId);
        }
#pragma warning restore IDE0008 // Use explicit type
    }

    /// <summary>
    /// Serializes network-visible state for identical-edit comparison.
    /// </summary>
    /// <param name="item">Item to compare, or null for an absent item.</param>
    /// <returns>Serialized DTO state, or null.</returns>
    private static string? Serialize(IItem? item)
    {
        return item is null
            ? null
            : ItemDtoStringSerializer.Serialize(ItemMapper.ToDto(item));
    }
}
