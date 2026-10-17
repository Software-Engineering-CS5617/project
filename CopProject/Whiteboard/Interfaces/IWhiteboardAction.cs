using System;

namespace Whiteboard.Interfaces;

public interface IWhiteboardAction
{
    Guid ActionId { get; set; }
    string UserId { get; set; }
    DateTime ActionDateTime { get; set; }

    void Perform(IWhiteboard whiteboard);
    IWhiteboardAction CreateInverse();
}