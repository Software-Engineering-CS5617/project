using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Windows.Foundation;
using Windows.UI;
using XamlEllipse = Microsoft.UI.Xaml.Shapes.Ellipse;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the ellipse tool is selected.
/// </summary>
public class ShapeEllipseState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly Canvas _canvas;
    private readonly ToolbarInfo _toolbarInfo;
    private bool _isDrawing;
    private Point _startPoint;
    private XamlEllipse? _ellipse;

    public ShapeEllipseState(
        IWhiteboardClient whiteboardClient,
        Canvas canvas,
        ToolbarInfo toolbarInfo)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(toolbarInfo);

        _whiteboardClient = whiteboardClient;
        _canvas = canvas;
        _toolbarInfo = toolbarInfo;
    }

    public void PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_canvas.CapturePointer(e.Pointer))
        {
            return;
        }

        _isDrawing = true;
        _startPoint = e.GetCurrentPoint(_canvas).Position;
        _ellipse = new XamlEllipse {
            Stroke = _toolbarInfo.BorderBrush,
            StrokeThickness = _toolbarInfo.StrokeThickness,
            Fill = _toolbarInfo.FillBrush,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(_ellipse, _startPoint.X);
        Canvas.SetTop(_ellipse, _startPoint.Y);
        _canvas.Children.Add(_ellipse);
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _ellipse is null)
        {
            return;
        }

        Point currentPoint = e.GetCurrentPoint(_canvas).Position;
        double left = Math.Min(_startPoint.X, currentPoint.X);
        double top = Math.Min(_startPoint.Y, currentPoint.Y);
        Canvas.SetLeft(_ellipse, left);
        Canvas.SetTop(_ellipse, top);
        _ellipse.Width = Math.Abs(currentPoint.X - _startPoint.X);
        _ellipse.Height = Math.Abs(currentPoint.Y - _startPoint.Y);
        e.Handled = true;
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        _canvas.ReleasePointerCapture(e.Pointer);

        if (_ellipse is not null)
        {
            double left = Canvas.GetLeft(_ellipse);
            double top = Canvas.GetTop(_ellipse);
            double width = _ellipse.Width;
            double height = _ellipse.Height;
            Color borderColor = _toolbarInfo.BorderBrush is SolidColorBrush borderBrush
                ? borderBrush.Color
                : Color.FromArgb(255, 0, 0, 0);
            Color fillColor = _toolbarInfo.FillBrush is SolidColorBrush fillBrush
                ? fillBrush.Color
                : Color.FromArgb(0, 0, 0, 0);
            Whiteboard.Items.Shapes.Ellipse ellipse = new() {
                Center = new Point2D(left + width / 2, top + height / 2),
                Width = width,
                Height = height,
                BorderColor = borderColor,
                BorderWidth = _toolbarInfo.StrokeThickness,
                FillColor = fillColor,
                CreatedBy = _whiteboardClient.UserId
            };

            _canvas.Children.Remove(_ellipse);
            _whiteboardClient.AddItem(ellipse);
        }

        _ellipse = null;
        e.Handled = true;
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        if (_ellipse is not null)
        {
            _canvas.Children.Remove(_ellipse);
        }

        _ellipse = null;
    }
}
