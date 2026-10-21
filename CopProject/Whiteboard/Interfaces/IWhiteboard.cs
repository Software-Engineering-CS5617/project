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
    void DeleteItem(Guid guid);
}
