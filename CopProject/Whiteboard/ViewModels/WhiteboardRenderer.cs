// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using XamlPolyline = Microsoft.UI.Xaml.Shapes.Polyline;
using XamlRectangle = Microsoft.UI.Xaml.Shapes.Rectangle;
using XamlEllipse = Microsoft.UI.Xaml.Shapes.Ellipse;
using XamlPolygon = Microsoft.UI.Xaml.Shapes.Polygon;
using Windows.Foundation;

namespace Whiteboard.ViewModels;

[ExcludeFromCodeCoverage]
internal sealed class WhiteboardRenderer : IDisposable
{
    private readonly IWhiteboard _board;
    private readonly Canvas _canvas;
    private readonly TextboxState _textboxState;
    private readonly Dictionary<Guid, Microsoft.UI.Xaml.UIElement> _visuals = [];

    public WhiteboardRenderer(IWhiteboard board, Canvas canvas, TextboxState textboxState)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(textboxState);

        _board = board;
        _canvas = canvas;
        _textboxState = textboxState;
        _board.ItemAdded += RenderAdded;
        _board.ItemUpdated += RenderUpdated;
        _board.ItemDeleted += RemoveDeleted;

        foreach (IItem item in _board.Items.Values)
        {
            RenderAdded(item);
        }
    }

    public void Dispose()
    {
        _board.ItemAdded -= RenderAdded;
        _board.ItemUpdated -= RenderUpdated;
        _board.ItemDeleted -= RemoveDeleted;
    }

    private void RenderAdded(IItem item)
    {
        RunOnUIThread(() => RenderAddedInternal(item));
    }

    private void RenderAddedInternal(IItem item)
    {
        RemoveDeletedInternal(item.Id);
        Microsoft.UI.Xaml.UIElement visual = CreateVisual(item);
        Canvas.SetZIndex(visual, (int)item.Zindex);
        _visuals[item.Id] = visual;
        _canvas.Children.Add(visual);

        // A freshly created empty textbox gets focus so the user can start typing
        if (visual is TextBox textBox && item is ITextbox { Content: "" })
        {
            textBox.Focus(FocusState.Programmatic);
        }
    }

    private void RenderUpdated(IItem item)
    {
        if (item is not ITextbox textbox)
        {
            RenderAdded(item);
            return;
        }

        RunOnUIThread(() => {
            // Rebuilding a TextBox would drop focus and whatever the user is typing,
            // so update the existing control in place instead.
            if (!_visuals.TryGetValue(textbox.Id, out Microsoft.UI.Xaml.UIElement? visual)
                || visual is not TextBox textBox)
            {
                RenderAddedInternal(textbox);
                return;
            }

            UpdateTextbox(textBox, textbox);
        });
    }

    /// <summary>
    /// Applies model changes to an existing TextBox without recreating it.
    /// The text is left alone while the user is editing it, so remote or
    /// echoed updates never overwrite unsaved typing.
    /// </summary>
    private static void UpdateTextbox(TextBox visual, ITextbox item)
    {
        if (visual.FocusState == FocusState.Unfocused && visual.Text != item.Content)
        {
            visual.Text = item.Content;
        }

        visual.Width = item.Width;
        visual.MinHeight = item.Height;
        visual.FontFamily = new FontFamily(item.FontFamily);
        visual.FontSize = item.FontSize;
        visual.Foreground = new SolidColorBrush(item.FontColor);
        Canvas.SetLeft(visual, item.BoundingBox.Left);
        Canvas.SetTop(visual, item.BoundingBox.Top);
        Canvas.SetZIndex(visual, (int)item.Zindex);
    }

    private void RemoveDeleted(Guid id)
    {
        RunOnUIThread(() => RemoveDeletedInternal(id));
    }

    private void RemoveDeletedInternal(Guid id)
    {
        if (_visuals.Remove(id, out Microsoft.UI.Xaml.UIElement? visual))
        {
            _canvas.Children.Remove(visual);
        }

        for (int index = _canvas.Children.Count - 1; index >= 0; index--)
        {
            if (_canvas.Children[index] is FrameworkElement element && element.Tag is Guid itemId && itemId == id)
            {
                _canvas.Children.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// Dispatch the given action on the UI thread
    /// </summary>
    /// <param name="action">Action to dispatch</param>
    private void RunOnUIThread(Action action)
    {
        _canvas.DispatcherQueue.TryEnqueue(() => action());
    }

    private Microsoft.UI.Xaml.UIElement CreateVisual(IItem item)
    {
        return item switch {
            IFreehand freehand => CreateFreehand(freehand),
            ITextbox textbox => CreateTextbox(textbox),
            Whiteboard.Items.Shapes.Square square => CreateSquare(square),
            Whiteboard.Items.Shapes.Circle circle => CreateCircle(circle),
            Whiteboard.Items.Shapes.Ellipse ellipse => CreateEllipse(ellipse),
            Whiteboard.Items.Shapes.EqTriangle triangle => CreateEqTriangle(triangle),
            _ => new Border()
        };
    }

    private TextBox CreateTextbox(ITextbox item)
    {
        TextBox visual = new() {
            Width = item.Width,

            // No fixed Height: the box grows with its content so every line is visible
            // without scrolling. The model height is only the starting (minimum) size.
            MinHeight = item.Height,
            Text = item.Content,
            FontFamily = new FontFamily(item.FontFamily),
            FontSize = item.FontSize,
            Foreground = new SolidColorBrush(item.FontColor),
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            IsHitTestVisible = true,
            Tag = item.Id
        };

        // Never show scrollbars; the box resizes to fit all lines instead
        ScrollViewer.SetVerticalScrollBarVisibility(visual, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollMode(visual, ScrollMode.Disabled);

        Canvas.SetLeft(visual, item.BoundingBox.Left);
        Canvas.SetTop(visual, item.BoundingBox.Top);
        ToolTipService.SetToolTip(visual, new ToolTip(item.CreatedBy, item.CreatedDateTime));

        // Track every keystroke locally; the view model decides when to send it
        // (on focus loss, tool switch or canvas press) through the whiteboard client.
        Guid itemId = item.Id;
        visual.TextChanged += (_, _) => _textboxState.UpdateText(itemId, visual.Text);
        visual.LostFocus += (_, _) => _textboxState.CommitPendingEdits();

        return visual;
    }

    private static XamlPolyline CreateFreehand(IFreehand item)
    {
        XamlPolyline visual = new() {
            Stroke = new SolidColorBrush(item.StrokeColor),
            StrokeThickness = item.StrokeWidth,
            StrokeLineJoin = Microsoft.UI.Xaml.Media.PenLineJoin.Round,
            StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
            StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
            IsHitTestVisible = true,
            Tag = item.Id
        };

        foreach (Point2D point in item.Points)
        {
            visual.Points.Add(new Point(point.X, point.Y));
        }

        ToolTipService.SetToolTip(visual, new ToolTip(item.CreatedBy, item.CreatedDateTime));
        return visual;
    }

    private static XamlPolygon CreateEqTriangle(Whiteboard.Items.Shapes.EqTriangle item)
    {
        double height = item.SideLength * Math.Sqrt(3) / 2;
        XamlPolygon visual = new() {
            Width = item.SideLength,
            Height = height,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = true,
            Tag = item.Id
        };

        visual.Points.Add(new Point(item.SideLength / 2, 0));
        visual.Points.Add(new Point(0, height));
        visual.Points.Add(new Point(item.SideLength, height));

        Canvas.SetLeft(visual, item.BoundingBox.Left);
        Canvas.SetTop(visual, item.BoundingBox.Top);
        Canvas.SetZIndex(visual, (int)item.Zindex);
        ToolTipService.SetToolTip(visual, new ToolTip(item.CreatedBy, item.CreatedDateTime));
        return visual;
    }

    private static XamlEllipse CreateCircle(Whiteboard.Items.Shapes.Circle item)
    {
        double diameter = item.Radius * 2;
        XamlEllipse visual = new() {
            Width = diameter,
            Height = diameter,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = true,
            Tag = item.Id
        };

        Canvas.SetLeft(visual, item.Center.X - item.Radius); 
        Canvas.SetTop(visual, item.Center.Y - item.Radius);
        ToolTipService.SetToolTip(visual, new ToolTip(item.CreatedBy, item.CreatedDateTime));
        return visual;
    }

    private static XamlEllipse CreateEllipse(Whiteboard.Items.Shapes.Ellipse item)
    {
        XamlEllipse visual = new() {
            Width = item.Width,
            Height = item.Height,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = true,
            Tag = item.Id
        };

        Canvas.SetLeft(visual, item.BoundingBox.Left);
        Canvas.SetTop(visual, item.BoundingBox.Top);
        ToolTipService.SetToolTip(visual, new ToolTip(item.CreatedBy, item.CreatedDateTime));
        return visual;
    }

    private static XamlRectangle CreateSquare(Whiteboard.Items.Shapes.Square item)
    {
        XamlRectangle visual = new() {
            Width = item.SideLegth,
            Height = item.SideLegth,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = true,
            Tag = item.Id
        };

        double left = item.Left;
        double top = item.Top;
        if (left == 0 && top == 0 && (item.Center.X != 0 || item.Center.Y != 0))
        {
            left = item.Center.X - (item.SideLegth / 2);
            top = item.Center.Y - (item.SideLegth / 2);
        }

        Canvas.SetLeft(visual, left);
        Canvas.SetTop(visual, top);
        ToolTipService.SetToolTip(visual, new ToolTip(item.CreatedBy, item.CreatedDateTime));
        return visual;
    }

    private static Windows.UI.Color ToColor(Windows.UI.Color color)
    {
        return color;
    }
}

