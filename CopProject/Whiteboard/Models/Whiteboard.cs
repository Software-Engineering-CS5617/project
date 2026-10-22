// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using Whiteboard.Interfaces;

namespace Whiteboard.Models;

/// <inheritdoc/>
public class Whiteboard : IWhiteboard
{
    public event Action<IItem> ItemAdded;
    public event Action<Guid> ItemDeleted;
    public event Action<IItem> ItemUpdated;

    private ConcurrentDictionary<Guid, IItem> _items = new();

    public IDictionary<Guid, IItem> Items { get => _items; set => _items = value; }

    public void AddItem(IItem item)
    {
        if (_items.TryAdd(item.Id, item))
        {
            ItemAdded?.Invoke(item);
        }
    }

    public bool DeleteItem(Guid guid)
    {
        if (_items.TryRemove(guid, out var removedItem))
        {
            ItemDeleted?.Invoke(guid);
            return true;
        }

        return false;
    }

    public bool EditItem(IItem item)
    {
        while (_items.TryGetValue(item.Id, out var existingItem))
        {
            if (_items.TryUpdate(item.Id, item, existingItem))
            {
                ItemUpdated?.Invoke(item);
                return true;
            }
        }

        return false;
    }
}
