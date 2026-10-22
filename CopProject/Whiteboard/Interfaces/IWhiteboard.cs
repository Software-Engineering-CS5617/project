using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Whiteboard.Interfaces;

/// <summary>
/// Represents a whiteboard which holds all whiteboard items
/// </summary>
public interface IWhiteboard
{
    /// <summary>
    /// Dictionary to hold all whiteboard items, keyed by their GUIDs
    /// </summary>
    IDictionary<Guid, IItem> Items { get; set; }

    /// <summary>
    /// Add the item to the whiteboard
    /// </summary>
    /// <param name="item">Item to add</param>
    void AddItem(IItem item);

    /// <summary>
    /// Remove the item from the whiteboard
    /// </summary>
    /// <param name="guid">GUID of the item to remove</param>
    /// <returns>true if the item was present in the whiteboard, otherwise false</returns>
    bool DeleteItem(Guid guid);

    /// <summary>
    /// Edit the item with new properties
    /// </summary>
    /// <param name="item">New item for replacement</param>
    /// <returns>true if an item with same GUID was present in the whiteboard, otherwise false</returns>
    bool EditItem(IItem item);
}
