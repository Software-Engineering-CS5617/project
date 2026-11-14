/// Owner   : Sasi Kiran
/// LICENSE : MIT

using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the ellipse tool is selected.
/// </summary>
public class ShapeEllipseState : ViewModelStateBase
{
    private Point2D _startPoint = new(0, 0);
    private IEllipse? _ellipse;

    public ShapeEllipseState(
        IWhiteboardClient whiteboardClient,
        ICanvasPreview preview,
        ToolbarInfo toolbarInfo) : base(whiteboardClient, preview, toolbarInfo)
    {
    }

    public override void PointerPressed(Point2D point)
    {
        _isDrawing = true;
        _startPoint = point;

        _ellipse = new Ellipse {
            CreatedBy = WhiteboardClient.UserId,
            BorderColor = ToolbarInfo.BorderColor,
            BorderWidth = ToolbarInfo.BorderWidth,
            FillColor = ToolbarInfo.FillColor,
            Center = new Point2D(point.X, point.Y),
            Width = 0,
            Height = 0,
            Zindex = GetNextZIndex()
        };

        Preview.ShowPreview(_ellipse);
    }

    public override void PointerMoved(Point2D point)
    {
        if (!_isDrawing || _ellipse is null)
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

        _ellipse.MoveBy(centerX - _ellipse.Center.X, centerY - _ellipse.Center.Y);
        _ellipse.Width = width;
        _ellipse.Height = height;

        Preview.ShowPreview(_ellipse);
    }

    public override void PointerReleased(Point2D point)
    {
        if (!_isDrawing || _ellipse is null)
        {
            return;
        }

        _isDrawing = false;
        Preview.ClearPreview();

        if (_ellipse.Width > 0 && _ellipse.Height > 0)
        {
            IEllipse item = _ellipse;
            Task.Run(() => WhiteboardClient.AddItem(item));
        }

        _ellipse = null;
    }

    public override void PointerCaptureLost(Point2D point)
    {
        if (!_isDrawing || _ellipse is null)
        {
            return;
        }

        _isDrawing = false;
        Preview.ClearPreview();
        _ellipse = null;
    }
}
