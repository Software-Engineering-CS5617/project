// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Interfaces;

namespace Whiteboard.Services;

public static class ItemSnapshot
{
    public static IItem Copy(IItem source)
    {
        ArgumentNullException.ThrowIfNull(source);

        IItem copy = source switch {
            FreehandItem item => new FreehandItem {
                Points = CopyPoints(item.Points),
                StrokeColor = item.StrokeColor,
                StrokeWidth = item.StrokeWidth,
                BoundingBox = CopyRect(item.BoundingBox)
            },

            Line item => new Line {
                Begin = CopyPoint(item.Begin),
                End = CopyPoint(item.End)
            },

            Textbox item => new Textbox {
                Center = CopyPoint(item.Center),
                Width = item.Width,
                Height = item.Height,
                Content = item.Content,
                FontFamily = item.FontFamily,
                FontSize = item.FontSize,
                FontColor = item.FontColor
            },

            Circle item => new Circle {
                Center = CopyPoint(item.Center),
                Radius = item.Radius
            },

            Ellipse item => new Ellipse {
                Center = CopyPoint(item.Center),
                Width = item.Width,
                Height = item.Height
            },

            EqTriangle item => new EqTriangle {
                Center = CopyPoint(item.Center),
                Left = item.Left,
                Top = item.Top,
                SideLength = item.SideLength,
                BoundingBox = CopyRect(item.BoundingBox)
            },

            Polygon item => new Polygon {
                Vertices = CopyPoints(item.Vertices)
            },

            Rectangle item => new Rectangle {
                Center = CopyPoint(item.Center),
                Width = item.Width,
                Height = item.Height
            },

            Square item => new Square {
                Center = CopyPoint(item.Center),
                SideLegth = item.SideLegth
            },

            _ => throw new NotSupportedException(
                $"Snapshots do not support {source.GetType().Name}.")
        };

        copy.Id = source.Id;
        copy.Zindex = source.Zindex;
        copy.CreatedBy = source.CreatedBy;
        copy.CreatedDateTime = source.CreatedDateTime;
        copy.LastModifiedDateTime = source.LastModifiedDateTime;
        copy.Left = source.Left;
        copy.Top = source.Top;
        copy.Orientation = source.Orientation;

        if (source is Shape sourceShape && copy is Shape copiedShape)
        {
            copiedShape.FillColor = sourceShape.FillColor;
            copiedShape.BorderColor = sourceShape.BorderColor;
            copiedShape.BorderWidth = sourceShape.BorderWidth;
        }

        return copy;
    }

    private static Point2D CopyPoint(Point2D point)
    {
        return point is null
            ? null
            : new Point2D(point.X, point.Y);
    }

    private static Rect2D CopyRect(Rect2D rectangle)
    {
        return rectangle is null
            ? null
            : new Rect2D(
                rectangle.Left,
                rectangle.Top,
                rectangle.Width,
                rectangle.Height);
    }

    private static List<Point2D> CopyPoints(List<Point2D> points)
    {
        return points?.Select(CopyPoint).ToList();
    }
}
