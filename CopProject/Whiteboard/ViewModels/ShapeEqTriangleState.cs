/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the equilateral triangle tool is selected.
/// </summary>
public class ShapeEqTriangleState : ViewModelStateBase
{
    private Point2D _startPoint = new(0, 0);
    private IEqTriangle? _triangle;

    public ShapeEqTriangleState(
        IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo) : base(whiteboardClient, preview, toolbarInfo)
    {
    }

    public override void PointerPressed(Point2D point)
    {
        _isDrawing = true;
        _startPoint = point;
        _triangle = new EqTriangle {
            CreatedBy = WhiteboardClient.UserId,

            BorderColor = ToolbarInfo.BorderColor,
            BorderWidth = ToolbarInfo.BorderWidth,
            FillColor = ToolbarInfo.FillColor,

            Center = new Point2D(_startPoint.X, _startPoint.Y),
            SideLength = 0,

            Zindex = GetNextZIndex()
        };

        Preview.ShowPreview(_triangle);
    }

    public override void PointerMoved(Point2D point)
    {
        if (!_isDrawing || _triangle is null)
        {
            return;
        }

        double horizontal = Math.Abs(point.X - _startPoint.X);
        double vertical = Math.Abs(point.Y - _startPoint.Y);
        double side = Math.Min(horizontal, vertical * 2.0 / Math.Sqrt(3.0));

        double height = side * Math.Sqrt(3.0) / 2.0;
        double boundingLeft = point.X >= _startPoint.X ? _startPoint.X : _startPoint.X - side;
        double boundingTop = point.Y >= _startPoint.Y ? _startPoint.Y : _startPoint.Y - height;

        double centerX = boundingLeft + (side / 2.0);
        double centerY = boundingTop + ((2.0 / 3.0) * height);

        _triangle.MoveBy(centerX - _triangle.Center.X, centerY - _triangle.Center.Y);
        _triangle.SideLength = side;

        Preview.ShowPreview(_triangle);
    }

    public override void PointerReleased(Point2D point)
    {
        if (!_isDrawing || _triangle is null)
        {
            return;
        }

        _isDrawing = false;
        Preview.ClearPreview();

        if (_triangle.SideLength > 0)
        {
            IEqTriangle item = _triangle;
            Task.Run(() => WhiteboardClient.AddItem(item));
        }

        _triangle = null;
    }

    public override void PointerCaptureLost(Point2D point)
    {
        if (!_isDrawing || _triangle is null)
        {
            return;
        }

        _isDrawing = false;
        Preview.ClearPreview();
        _triangle = null;
    }
}
