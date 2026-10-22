using System;
using Whiteboard.Interfaces;

namespace Whiteboard.Actions;

public class EraseAction : IWhiteboardAction
{
    public Guid ActionId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; }
    public DateTime ActionDateTime { get; set; } = DateTime.UtcNow;

    private readonly IItem _item;

    public EraseAction(IItem item, string userId)
    {
        _item = item;
        UserId = userId;
    }

    public void Perform(IWhiteboard whiteboard)
    {
        whiteboard.DeleteItem(_item.Id);
    }

    public IWhiteboardAction CreateInverse()
    {
        if(_item is IFreehand freehand)
        {
            return new DrawFreehandAction(freehand, UserId);
        }
        return null;
    }
}