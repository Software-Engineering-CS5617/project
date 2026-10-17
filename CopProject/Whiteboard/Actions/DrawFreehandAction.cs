using System;
using Whiteboard.Interfaces;

namespace Whiteboard.Actions;

public class DrawFreehandAction : IWhiteboardAction
{
    public Guid ActionId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; }
    public DateTime ActionDateTime { get; set; } = DateTime.UtcNow;

    private readonly IFreehand _item;

    public DrawFreehandAction(IFreehand item, string userId)
    {
        _item = item;
        UserId = userId;
    }

    public void Perform(IWhiteboard whiteboard)
    {
        whiteboard.AddItem(_item);
    }

    public IWhiteboardAction CreateInverse()
    {
        return new EraseAction(_item, UserId);
    }
}