// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/****
    * Filename  : AddItemAction.cs
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
/// Adds one or more items as a single reversible action.
/// </summary>
public class AddItemAction : IWhiteboardAction
{
    private readonly IItem[] _snapshots;

    /// <inheritdoc/>
    public Guid ActionId { get; set; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string UserId { get; set; }

    /// <inheritdoc/>
    public DateTime ActionDateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates an action for adding one item.
    /// </summary>
    /// <param name="item">Item to add.</param>
    /// <param name="userId">User performing the action.</param>
    public AddItemAction(IItem item, string userId)
        : this(new[] { item }, userId)
    {
    }

    /// <summary>
    /// Creates an action for adding a group of items.
    /// </summary>
    /// <param name="items">Items to add together.</param>
    /// <param name="userId">User performing the action.</param>
    public AddItemAction(IEnumerable<IItem> items, string userId)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        IItem[] suppliedItems = items.ToArray();

        if (suppliedItems.Length == 0)
        {
            Trace.TraceError($"[AddItemAction][Error] Cannot create an empty add action. User={userId}, Action={ActionId}");
            throw new ArgumentException(
                "An add action needs at least one item.",
                nameof(items));
        }

        if (suppliedItems.Any(item => item is null))
        {
            Trace.TraceError($"[AddItemAction][Error] Add action contains a null item. User={userId}, Action={ActionId}");
            throw new ArgumentException(
                "An add action cannot contain a null item.",
                nameof(items));
        }

        if (suppliedItems.Select(item => item.Id).Distinct().Count()
            != suppliedItems.Length)
        {
            Trace.TraceError($"[AddItemAction][Error] Addition rejected: item already exists. User={UserId}, Action={ActionId}");
            throw new ArgumentException(
                "An add action cannot contain duplicate item IDs.",
                nameof(items));
        }

        UserId = userId;

        // Save copies before the caller can change the original items.
        _snapshots = suppliedItems.Select(ItemSnapshot.Copy).ToArray();
    }

    /// <summary>
    /// Adds fresh copies of the saved items to the board.
    /// </summary>
    /// <param name="whiteboard">Board receiving the items.</param>
    public void Perform(IWhiteboard whiteboard)
    {
        ArgumentNullException.ThrowIfNull(whiteboard);

        // Check the whole group before adding anything.
        foreach (IItem snapshot in _snapshots)
        {
            if (whiteboard.Items.ContainsKey(snapshot.Id))
            {
                Trace.TraceError($"[AddItemAction][Error] Addition rejected: item already exists. User={UserId}, Action={ActionId}, Item={snapshot.Id}");
                throw new InvalidOperationException(
                    $"Item {snapshot.Id} already exists.");
            }
        }

        // Keep editable board items separate from history snapshots.
        IItem[] itemsToAdd = _snapshots
            .Select(ItemSnapshot.Copy)
            .ToArray();

        foreach (IItem item in itemsToAdd)
        {
            whiteboard.AddItem(item);
        }
        Trace.TraceInformation($"[AddItemAction] Addition completed. User={UserId}, Action={ActionId}, Items={itemsToAdd.Length}");
    }

    /// <summary>
    /// Creates an action that deletes the added items.
    /// </summary>
    /// <returns>The corresponding delete action.</returns>
    public IWhiteboardAction CreateInverse()
    {
        return new DeleteItemsAction(
            _snapshots.Select(item => item.Id),
            UserId);
    }
}
