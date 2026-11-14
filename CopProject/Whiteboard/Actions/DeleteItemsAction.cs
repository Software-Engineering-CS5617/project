// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/****
    * Filename  : DeleteItemsAction.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Whiteboard.Interfaces;
using Whiteboard.Services;
using System.Diagnostics;

namespace Whiteboard.Actions;

/// <summary>
/// Deletes items and saves their state for undo.
/// </summary>
public class DeleteItemsAction : IWhiteboardAction
{
    private readonly Guid[] _itemIds;
    private IItem[] _deletedSnapshots = Array.Empty<IItem>();

    /// <inheritdoc/>
    public Guid ActionId { get; set; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string UserId { get; set; }

    /// <inheritdoc/>
    public DateTime ActionDateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates an action for deleting one item.
    /// </summary>
    /// <param name="itemId">ID of the item to delete.</param>
    /// <param name="userId">User performing the action.</param>
    public DeleteItemsAction(Guid itemId, string userId)
        : this(new[] { itemId }, userId)
    {
    }

    /// <summary>
    /// Creates an action for deleting a group of items.
    /// </summary>
    /// <param name="itemIds">IDs of the items to delete.</param>
    /// <param name="userId">User performing the action.</param>
    public DeleteItemsAction(IEnumerable<Guid> itemIds, string userId)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        _itemIds = itemIds.Distinct().ToArray();

        if (_itemIds.Length == 0)
        {
            Trace.TraceError($"[DeleteItemsAction][Error] Cannot create an empty delete action. User={userId}, Action={ActionId}");
            throw new ArgumentException(
                "A delete action needs at least one item ID.",
                nameof(itemIds));
        }

        UserId = userId;
    }

    /// <summary>
    /// Copies the requested items and removes them from the board.
    /// </summary>
    /// <param name="whiteboard">Board containing the items.</param>
    public void Perform(IWhiteboard whiteboard)
    {
        ArgumentNullException.ThrowIfNull(whiteboard);

        var snapshots = new List<IItem>();

        // Find and copy every item before deleting any of them.
        foreach (Guid id in _itemIds)
        {
            if (!whiteboard.Items.TryGetValue(id, out IItem item))
            {
                Trace.TraceError($"[DeleteItemsAction][Error] Deletion rejected: item does not exist. User={UserId}, Action={ActionId}, Item={id}");
                throw new InvalidOperationException(
                    $"Item {id} does not exist.");
            }

            snapshots.Add(ItemSnapshot.Copy(item));
        }

        foreach (Guid id in _itemIds)
        {
            if (!whiteboard.DeleteItem(id))
            {
                Trace.TraceError($"[DeleteItemsAction][Error] Item could not be deleted. User={UserId}, Action={ActionId}, Item={id}");
                throw new InvalidOperationException(
                    $"Item {id} could not be deleted.");
            }
        }

        _deletedSnapshots = snapshots.ToArray();
        Trace.TraceInformation($"[DeleteItemsAction] Deletion completed. User={UserId}, Action={ActionId}, Items={_deletedSnapshots.Length}");
    }

    /// <summary>
    /// Creates an action that restores the deleted items.
    /// </summary>
    /// <returns>An add action containing the saved items.</returns>
    public IWhiteboardAction CreateInverse()
    {
        if (_deletedSnapshots.Length == 0)
        {
            Trace.TraceInformation($"[DeleteItemsAction] Deletion completed. User={UserId}, Action={ActionId}, Items={_deletedSnapshots.Length}");
            throw new InvalidOperationException(
                "Perform the deletion before creating its inverse.");
        }

        return new AddItemAction(_deletedSnapshots, UserId);
    }
}
