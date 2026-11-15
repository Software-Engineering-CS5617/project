//owner : U Sai Vignesh
//roll number : 112301034
// License : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Services;

namespace Whiteboard.Selection;

[ExcludeFromCodeCoverage]
// Temporary exclusion from code coverage
public class SelectionService : ISelectionService
{
    private readonly IWhiteboard _canvas;
    private readonly ISelectionState _selectionState;

    public SelectionService(IWhiteboard canvas, ISelectionState selectionState)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _selectionState = selectionState ?? throw new ArgumentNullException(nameof(selectionState));
    }

    public bool SelectAtPoint(Point2D point)
    {
        IItem? hitItem = _canvas.HitTestTopmost(point);

        if (hitItem is null)
        {
            _selectionState.Clear();
            return false;
        }

        _selectionState.SelectSingle(hitItem.Id);
        return true;
    }

    /// <summary>
    /// Moves every selected item on the local canvas only.
    /// Sending the result to the server is left to the caller, so a drag
    /// can update the screen on every pointer move but be sent just once.
    /// </summary>
    public bool MoveSelected(double dx, double dy)
    {
        bool moved = false;

        foreach (Guid id in _selectionState.SelectedItemIds)
        {
            if (!_canvas.Items.TryGetValue(id, out IItem? item))
            {
                continue;
            }

            // Move a copy so the item object held by the canvas (and its history) is never mutated
            IItem copy = ItemSnapshot.Copy(item);
            copy.MoveBy(dx, dy);
            copy.LastModifiedDateTime = DateTime.Now;
            moved |= _canvas.EditItem(copy);
        }

        return moved;
    }

    public bool ResizeSelected(IItem originalItem, Point2D pointer)
    {
        ArgumentNullException.ThrowIfNull(originalItem);

        if (!_selectionState.IsSelected(originalItem.Id) ||
            !_canvas.Items.ContainsKey(originalItem.Id))
        {
            return false;
        }

        Rect2D box = originalItem.BoundingBox;
        double targetWidth = Math.Max(1, pointer.X - box.Left);
        double targetHeight = Math.Max(1, pointer.Y - box.Top);
        double scaleX = box.Width > 0 ? targetWidth / box.Width : 1;
        double scaleY = box.Height > 0 ? targetHeight / box.Height : 1;
        IItem resized = ItemSnapshot.Copy(originalItem);

        if (!ScaleItem(resized, box, scaleX, scaleY))
        {
            return false;
        }

        resized.LastModifiedDateTime = DateTime.Now;
        return _canvas.EditItem(resized);
    }

    public bool RotateSelected(double deltaAngle)
    {
        throw new NotImplementedException("RotateSelected is not implemented yet.");
    }


    private static bool ScaleItem(IItem item, Rect2D originalBox, double scaleX, double scaleY)
    {
        if (item is Textbox textbox)
        {
            textbox.Width = originalBox.Width * scaleX;
            textbox.Height = originalBox.Height * scaleY;
            textbox.Center = ScalePoint(textbox.Center, originalBox, scaleX, scaleY);
            return true;
        }

        if (item is Line line)
        {
            line.Begin = ScalePoint(line.Begin, originalBox, scaleX, scaleY);
            line.End = ScalePoint(line.End, originalBox, scaleX, scaleY);
            return true;
        }

        if (item is Rectangle rectangle)
        {
            rectangle.Width = originalBox.Width * scaleX;
            rectangle.Height = originalBox.Height * scaleY;
            rectangle.Center = ScalePoint(rectangle.Center, originalBox, scaleX, scaleY);
            return true;
        }

        if (item is Square square)
        {
            double factor = Math.Min(scaleX, scaleY);
            square.SideLegth = originalBox.Width * factor;
            square.Center = ScalePoint(square.Center, originalBox, factor, factor);
            return true;
        }

        if (item is Circle circle)
        {
            double factor = Math.Min(scaleX, scaleY);
            circle.Radius = (originalBox.Width * factor) / 2;
            circle.Center = ScalePoint(circle.Center, originalBox, factor, factor);
            return true;
        }

        if (item is Ellipse ellipse)
        {
            ellipse.Width = originalBox.Width * scaleX;
            ellipse.Height = originalBox.Height * scaleY;
            ellipse.Center = ScalePoint(ellipse.Center, originalBox, scaleX, scaleY);
            return true;
        }

        if (item is EqTriangle eqTriangle)
        {
            double factor = Math.Min(scaleX, scaleY);
            eqTriangle.SideLength = originalBox.Width * factor;
            eqTriangle.Center = ScalePoint(eqTriangle.Center, originalBox, factor, factor);
            return true;
        }

        if (item is FreehandItem freehand)
        {
            for (int index = 0; index < freehand.Points.Count; index++)
            {
                freehand.Points[index] = ScalePoint(freehand.Points[index], originalBox, scaleX, scaleY);
            }

            return true;
        }

        if (item is Polygon polygon)
        {
            for (int index = 0; index < polygon.Vertices.Count; index++)
            {
                polygon.Vertices[index] = ScalePoint(polygon.Vertices[index], originalBox, scaleX, scaleY);
            }

            return true;
        }

        return false;
    }

    private static Point2D ScalePoint(Point2D point, Rect2D box, double scaleX, double scaleY)
    {
        return new Point2D(
            box.Left + ((point.X - box.Left) * scaleX),
            box.Top + ((point.Y - box.Top) * scaleY));
    }
}
