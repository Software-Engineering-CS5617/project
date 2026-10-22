using System;
using System.Collections.Generic;
using System.Linq;
using Whiteboard.Interfaces;

namespace Whiteboard;

public class WhiteboardCanvas : IWhiteboard
{
    public IDictionary<Guid, IItem> Items { get; set; } = new Dictionary<Guid, IItem>();

    public void AddItem(IItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        Items[item.Id] = item;
    }

    public bool DeleteItem(Guid guid)
    {
        return Items.Remove(guid);
    }

    public bool EditItem(IItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (!Items.ContainsKey(item.Id))
        {
            return false;
        }

        Items[item.Id] = item;
        return true;
    }

    // Optional compatibility helper if older call-sites still use RemoveItem
    public bool RemoveItem(IItem item)
    {
        if (item is null)
        {
            return false;
        }

        return DeleteItem(item.Id);
    }

    // Selection helper: topmost item at a point (highest Zindex wins)
    public IItem HitTestTopmost(Point2D point)
    {
        return Items.Values
            .Where(i => i.HitTest(point))
            .OrderByDescending(i => i.Zindex)
            .FirstOrDefault();
    }

    // Selection helper: all items at a point, sorted top-first
    public List<IItem> HitTestAll(Point2D point)
    {
        return Items.Values
            .Where(i => i.HitTest(point))
            .OrderByDescending(i => i.Zindex)
            .ToList();
    }

    // Selection helper: items whose bounding boxes overlap a region
    public List<IItem> InRect(Rect2D rect)
    {
        return Items.Values
            .Where(i => Overlaps(i.BoundingBox, rect))
            .OrderByDescending(i => i.Zindex)
            .ToList();
    }

    private static bool Overlaps(Rect2D a, Rect2D b)
    {
        return a.Left < b.Left + b.Width &&
               a.Left + a.Width > b.Left &&
               a.Top < b.Top + b.Height &&
               a.Top + a.Height > b.Top;
    }
}
