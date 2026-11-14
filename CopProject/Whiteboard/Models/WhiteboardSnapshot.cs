using System;
using System.Collections.Generic;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Models;

public class WhiteboardSnapshot
{
    public string SessionId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string SavedBy { get; set; } = string.Empty;
    public DateTimeOffset SavedAt { get; set; }
    public List<ItemDto> Items { get; set; } = new();
}

public class WhiteboardSnapshotSummary
{
    public string SnapshotId { get; set; } = string.Empty;
    public string SavedBy { get; set; } = string.Empty;
    public DateTimeOffset SavedAt { get; set; }

    public override string ToString()
    {
        return $"{SavedAt.LocalDateTime} - {SavedBy}";
    }
}