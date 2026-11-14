/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Windows.Foundation;
using XamlEllipse = Microsoft.UI.Xaml.Shapes.Ellipse;
using XamlPolygon = Microsoft.UI.Xaml.Shapes.Polygon;
using XamlPolyline = Microsoft.UI.Xaml.Shapes.Polyline;
using XamlRectangle = Microsoft.UI.Xaml.Shapes.Rectangle;

namespace Whiteboard.Views;

/// <inheritdoc/>
[ExcludeFromCodeCoverage]
public class CanvasPreview : ICanvasPreview
{
    /// <summary>
    /// Canvas to preview items on
    /// </summary>
    private readonly Canvas _canvas;

    /// <summary>
    /// UI item to preview on the canvas
    /// </summary>
    private UIElement? _previewElement;

    public CanvasPreview(Canvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _canvas = canvas;
    }

    /// <inheritdoc/>
    public void ShowPreview(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        RunOnUIThread(() => {
            if (_previewElement is not null)
            {
                _canvas.Children.Remove(_previewElement);
                _previewElement = null;
            }

            _previewElement = CreatePreviewVisual(item);
            if (_previewElement is not null)
            {
                Canvas.SetZIndex(_previewElement, (int)item.Zindex);
                _canvas.Children.Add(_previewElement);
            }
        });
    }

    /// <inheritdoc/>
    public void ClearPreview()
    {
        RunOnUIThread(() => {
            if (_previewElement is not null)
            {
                _canvas.Children.Remove(_previewElement);
                _previewElement = null;
            }
        });
    }

    /// <summary>
    /// A helper function to run the <paramref name="action"/> on the UI thread
    /// </summary>
    /// <param name="action">Action to run on the UI thread</param>
    private void RunOnUIThread(Action action)
    {
        if (_canvas.DispatcherQueue is null || _canvas.DispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            _canvas.DispatcherQueue.TryEnqueue(() => action());
        }
    }

    /// <summary>
    /// Create a UI element out of the given <paramref name="item"/> model
    /// </summary>
    /// <param name="item">Item to create UI element from</param>
    /// <returns>UI element for the given item</returns>
    private static UIElement? CreatePreviewVisual(IItem item)
    {
        return item switch {
            IFreehand freehand => CreateFreehandVisual(freehand),
            ISquare square => CreateSquareVisual(square),
            ICircle circle => CreateCircleVisual(circle),
            IEqTriangle triangle => CreateEqTriangleVisual(triangle),
            _ => null
        };
    }

    /// <summary>
    /// Helper function to create a polyline from a freehand item
    /// </summary>
    /// <param name="item">Freehand item</param>
    /// <returns>UI polyline</returns>
    private static XamlPolyline CreateFreehandVisual(IFreehand item)
    {
        XamlPolyline visual = new() {
            Stroke = new SolidColorBrush(item.StrokeColor),
            StrokeThickness = item.StrokeWidth,

            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,

            // disable hittesting for previews
            IsHitTestVisible = false
        };

        foreach (Point2D point in item.Points)
        {
            visual.Points.Add(new Point(point.X, point.Y));
        }

        return visual;
    }

    /// <summary>
    /// Helper function to create a square from a square item
    /// </summary>
    /// <param name="item">Square item</param>
    /// <returns>UI square as a rectangle</returns>
    private static XamlRectangle CreateSquareVisual(ISquare item)
    {
        XamlRectangle visual = new() {
            Width = item.SideLegth,
            Height = item.SideLegth,

            Fill = new SolidColorBrush(item.FillColor),
            Stroke = new SolidColorBrush(item.BorderColor),
            StrokeThickness = item.BorderWidth,

            // disable hittesting for previews
            IsHitTestVisible = false
        };

        Canvas.SetLeft(visual, item.Center.X - (item.SideLegth / 2.0));
        Canvas.SetTop(visual, item.Center.Y - (item.SideLegth / 2.0));

        return visual;
    }

    /// <summary>
    /// Helper function to create a circle from a circle item
    /// </summary>
    /// <param name="item">Circle item</param>
    /// <returns>UI circle as an ellipse</returns>
    private static XamlEllipse CreateCircleVisual(ICircle item)
    {
        double diameter = item.Radius * 2.0;
        XamlEllipse visual = new() {
            Width = diameter,
            Height = diameter,

            Fill = new SolidColorBrush(item.FillColor),
            Stroke = new SolidColorBrush(item.BorderColor),
            StrokeThickness = item.BorderWidth,

            // disable hittesting for previews
            IsHitTestVisible = false
        };

        Canvas.SetLeft(visual, item.Center.X - item.Radius);
        Canvas.SetTop(visual, item.Center.Y - item.Radius);

        return visual;
    }

    /// <summary>
    /// Helper function to create an equilateral triangle from an equilateral triangle item
    /// </summary>
    /// <param name="item">Equilateral triangle item</param>
    /// <returns>UI polygon which resembles an equilateral triangle</returns>
    private static XamlPolygon CreateEqTriangleVisual(IEqTriangle item)
    {
        double height = item.SideLength * Math.Sqrt(3.0) / 2.0;
        XamlPolygon visual = new() {
            Width = item.SideLength,
            Height = height,

            Fill = new SolidColorBrush(item.FillColor),
            Stroke = new SolidColorBrush(item.BorderColor),
            StrokeThickness = item.BorderWidth,

            // disable hittesting for previews
            IsHitTestVisible = false
        };

        visual.Points.Add(new Point(item.SideLength / 2.0, 0));
        visual.Points.Add(new Point(0, height));
        visual.Points.Add(new Point(item.SideLength, height));

        Canvas.SetLeft(visual, item.BoundingBox.Left);
        Canvas.SetTop(visual, item.BoundingBox.Top);

        return visual;
    }
}
