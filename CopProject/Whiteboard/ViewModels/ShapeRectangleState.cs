/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the rectangle tool is selected.
/// </summary>
public class ShapeRectangleState : ViewModelStateBase
{
    private Point2D _startPoint = new(0, 0);
    private IRectangle? _rectangle;

    public ShapeRectangleState(
        IWhiteboardClient whiteboardClient,
        ICanvasPreview preview,
        ToolbarInfo toolbarInfo) : base(whiteboardClient, preview, toolbarInfo)
    {
    }

    public override void PointerPressed(Point2D point)
    {
        _isDrawing = true;
        _startPoint = point;

        _rectangle = new Rectangle {
            CreatedBy = WhiteboardClient.UserId,
            BorderColor = ToolbarInfo.BorderColor,
            BorderWidth = ToolbarInfo.BorderWidth,
            FillColor = ToolbarInfo.FillColor,
            Center = new Point2D(point.X, point.Y),
            Width = 0,
            Height = 0,
            Zindex = GetNextZIndex()
        };

        Preview.ShowPreview(_rectangle);
    }

    public override void PointerMoved(Point2D point)
    {
        if (!_isDrawing || _rectangle is null)
        {
            return;
        }

        double width = System.Math.Abs(point.X - _startPoint.X);
        double height = System.Math.Abs(point.Y - _startPoint.Y);
        double centerX = point.X >= _startPoint.X
            ? _startPoint.X + (width / 2.0)
            : _startPoint.X - (width / 2.0);
        double centerY = point.Y >= _startPoint.Y
            ? _startPoint.Y + (height / 2.0)
            : _startPoint.Y - (height / 2.0);

        _rectangle.MoveBy(centerX - _rectangle.Center.X, centerY - _rectangle.Center.Y);
        _rectangle.Width = width;
        _rectangle.Height = height;

        Preview.ShowPreview(_rectangle);
    }

    public override void PointerReleased(Point2D point)
    {
        if (!_isDrawing || _rectangle is null)
        {
            return;
        }

        _isDrawing = false;
        Preview.ClearPreview();

        if (_rectangle.Width > 0 && _rectangle.Height > 0)
        {
            IRectangle item = _rectangle;
            Task.Run(() => WhiteboardClient.AddItem(item));
        }

        _rectangle = null;
    }

    public override void PointerCaptureLost(Point2D point)
    {
        if (!_isDrawing || _rectangle is null)
        {
            return;
        }

        _isDrawing = false;
        Preview.ClearPreview();
        _rectangle = null;
    }
}
