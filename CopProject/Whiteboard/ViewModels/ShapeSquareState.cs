/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Windows.Foundation;
using Windows.UI;
using Square = Whiteboard.Items.Shapes.Square;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Shape Square tool is selected
/// </summary>
[ExcludeFromCodeCoverage]
// Temporary exclusion from code coverage
public class ShapeSquareState : IViewModelState
{
    /// <summary>
    /// The whiteboard client
    /// </summary>
    private readonly IWhiteboardClient _whiteboardClient;

    /// <summary>
    /// The canvas UI
    /// </summary>
    private readonly Canvas _canvas;

    /// <summary>
    /// Current toolbar information
    /// </summary>
    private readonly ToolbarInfo _toolbarInfo;

    /// <summary>
    /// Flag to check whether the user is currently drawing on screen
    /// </summary>
    private bool _isDrawing = false;

    /// <summary>
    /// Start point of the square
    /// </summary>
    private Point _startPoint;

    /// <summary>
    /// Active square
    /// </summary>
    private Rectangle _square;

    public ShapeSquareState(IWhiteboardClient whiteboardClient, Canvas canvas, ToolbarInfo toolbarInfo)
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
        // Try capturing the pointer
        if (!_canvas.CapturePointer(e.Pointer))
        {
            return;
        }

        // Set flag to begin drawing
        _isDrawing = true;

        // Record the location where the pointer was pressed
        _startPoint = e.GetCurrentPoint(_canvas).Position;

        // Create a new square with brush and fill properties taken from the toolbar
        _square = new Rectangle {
            Stroke = _toolbarInfo.BorderBrush,
            StrokeThickness = _toolbarInfo.StrokeThickness,
            Fill = _toolbarInfo.FillBrush,
            IsHitTestVisible = false
        };

        // Set the top, left, width and height properties of the square
        Canvas.SetLeft(_square, _startPoint.X);
        Canvas.SetTop(_square, _startPoint.Y);
        _square.Width = 0;
        _square.Height = 0;

        // Add the square to the canvas for rendering
        _canvas.Children.Add(_square);

        // Mark the event as handled
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        // Return early if the user is not drawing
        if (!_isDrawing)
        {
            return;
        }

        if (_square is not null)
        {
            // Get the current pointer position
            Point currentPoint = e.GetCurrentPoint(_canvas).Position;

            // Compute side length as the minimum of the X and Y distances
            double side = Math.Min(
                Math.Abs(currentPoint.X - _startPoint.X),
                Math.Abs(currentPoint.Y - _startPoint.Y)
            );

            // Adjust X and Y based on drag direction (quadrants)
            double x = currentPoint.X >= _startPoint.X ? _startPoint.X : _startPoint.X - side;
            double y = currentPoint.Y >= _startPoint.Y ? _startPoint.Y : _startPoint.Y - side;

            // Apply position and equal width/height
            Canvas.SetLeft(_square, x);
            Canvas.SetTop(_square, y);
            _square.Width = side;
            _square.Height = side;
        }

        // Mark the event as handled
        e.Handled = true;
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Return early if the user is not drawing
        if (!_isDrawing)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        // Release the captured pointer
        _canvas.ReleasePointerCapture(e.Pointer);

        if (_square is not null)
        {
            double left = Canvas.GetLeft(_square);
            double top = Canvas.GetTop(_square);
            double side = _square.Width;

            // Get the brush color for stroke and fill
            // TODO too complicated, simplify
            Color strokeColor = _toolbarInfo.BorderBrush is SolidColorBrush sb ? sb.Color : Color.FromArgb(255, 0, 0, 0);
            Color fillColor = _toolbarInfo.FillBrush is SolidColorBrush fb ? fb.Color : Color.FromArgb(0, 0, 0, 0);

            // Create a freehand item to sync with client
            var square = new Square {
                Left = left,
                Top = top,
                SideLegth = side,
                Center = new Point2D(left + side / 2, top + side / 2),
                BorderColor = strokeColor,
                BorderWidth = _toolbarInfo.StrokeThickness,
                FillColor = fillColor,
                CreatedBy = _whiteboardClient.UserId
            };

            _canvas.Children.Remove(_square);

            // Send to whiteboard client state manager off the UI thread
            _whiteboardClient.AddItem(square);
        }

        // Reset square state back to null
        _square = null;

        // Mark the event as handled
        e.Handled = true;
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        // Return early if the user is not drawing
        if (!_isDrawing)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        if (_square is not null)
        {
            // Remove partially formed square from the canvas
            _canvas.Children.Remove(_square);
        }

        // Reset square state back to null
        _square = null;
    }
}
