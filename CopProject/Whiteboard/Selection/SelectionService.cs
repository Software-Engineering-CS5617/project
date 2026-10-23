// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;

namespace Whiteboard.Selection;

public class SelectionService
{
    private readonly WhiteboardCanvas _canvas;
    private readonly SelectionState _selectionState;

    public SelectionService(WhiteboardCanvas canvas, SelectionState selectionState)
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

    public bool MoveSelected(double dx, double dy, string modifiedBy = "system")
    {
        throw new NotImplementedException("MoveSelected is not implemented yet.");
    }

    public bool RotateSelected(double deltaAngle, string modifiedBy = "system")
    {
        throw new NotImplementedException("RotateSelected is not implemented yet.");
    }


    private static bool ScaleItem(IItem item, double factor)
    {
        if (item is Textbox textbox)
        {
            textbox.Width *= factor;
            textbox.Height *= factor;
            return true;
        }

        if (item is Line line)
        {
            line.End = new Point2D {
                X = line.Begin.X + ((line.End.X - line.Begin.X) * factor),
                Y = line.Begin.Y + ((line.End.Y - line.Begin.Y) * factor)
            };
            return true;
        }

        if (item is Rectangle rectangle)
        {
            rectangle.Width *= factor;
            rectangle.Height *= factor;
            return true;
        }

        if (item is Square square)
        {
            square.SideLegth *= factor;
            return true;
        }

        if (item is Circle circle)
        {
            circle.Radius *= factor;
            return true;
        }

        if (item is Ellipse ellipse)
        {
            ellipse.Width *= factor;
            ellipse.Height *= factor;
            return true;
        }

        if (item is EqTriangle eqTriangle)
        {
            eqTriangle.SideLength *= factor;
            return true;
        }

        if (item is FreehandItem freehand)
        {
            throw new NotImplementedException("Scaling FreehandItem is not implemented yet.");
        }

        return false;
    }
}
