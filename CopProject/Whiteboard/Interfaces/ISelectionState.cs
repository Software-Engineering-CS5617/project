using System;
using System.Collections.Generic;

namespace Whiteboard.Interfaces;

/// <summary>
/// Represents the current selection state of a whiteboard.
/// </summary>
public interface ISelectionState
{
    /// <summary>
    /// IDs of the currently selected items.
    /// </summary>
    HashSet<Guid> SelectedItemIds { get; }

    /// <summary>
    /// ID of the primary selected item, if one exists.
    /// </summary>
    Guid? PrimarySelectedId { get; }

    /// <summary>
    /// Clears all selected items.
    /// </summary>
    void Clear();

    /// <summary>
    /// Selects one item and clears the previous selection.
    /// </summary>
    /// <param name="id">ID of the item to select.</param>
    void SelectSingle(Guid id);

    /// <summary>
    /// Adds an item to the selection or removes it if it is already selected.
    /// </summary>
    /// <param name="id">ID of the item to toggle.</param>
    void Toggle(Guid id);

    /// <summary>
    /// Determines whether an item is selected.
    /// </summary>
    /// <param name="id">ID of the item to check.</param>
    /// <returns><see langword="true"/> when the item is selected; otherwise, <see langword="false"/>.</returns>
    bool IsSelected(Guid id);
}
