/****
    * Filename  : HistoryEntryDto.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/


using System;
using System.Collections.Generic;
using System.Text;

namespace Whiteboard.Network.Dtos;

/// <summary>
/// Represents one undo/redo history entry sent to a client.
/// </summary>
/// <param name="ItemId">ID of the affected item.</param>
/// <param name="Before">
/// Item state before the operation, or null if the item was absent.
/// </param>
/// <param name="After">
/// Item state after the operation, or null if the item was deleted.
/// </param>
public sealed record HistoryEntryDto(
    Guid ItemId,
    ItemDto? Before,
    ItemDto? After);
