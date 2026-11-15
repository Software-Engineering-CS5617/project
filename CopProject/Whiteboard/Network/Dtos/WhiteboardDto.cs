/// Owners  : Raagam Hitesh Parmar 
/// Roll No : 112301024
/// LICENSE : MIT

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.Network.Dtos;

/// <summary>
/// Describes a serializable representation of the whiteboard state
/// </summary>
/// <param name="Items">List of items in the whiteboard</param>
[ExcludeFromCodeCoverage]
public record WhiteboardDto(
    List<ItemDto> Items
 );
