        using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the textbox tool is selected.
/// </summary>
public sealed class TextboxState : ViewModelStateBase
{
    public TextboxState(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo)
        : base(whiteboardClient, preview, toolbarInfo)
    {
    }

    public override void PointerPressed(Point2D point)
    {
        Textbox textbox = new() {
            Center = point,
            CreatedBy = WhiteboardClient.UserId,
            Zindex = GetNextZIndex()
        };

        WhiteboardClient.AddItem(textbox);
    }

    public override void PointerMoved(Point2D point)
    {
    }

    public override void PointerReleased(Point2D point)
    {
    }

    public override void PointerCaptureLost(Point2D point)
    {
    }
}
