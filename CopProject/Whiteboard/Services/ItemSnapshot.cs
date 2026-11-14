// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/****
    * Filename  : ItemSnapshot.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.Services;

/// <summary>
/// Creates independent copies of items for undo and redo.
/// </summary>
[ExcludeFromCodeCoverage]
// Temporary exclusion from code coverage
public static class ItemSnapshot
{
    /// <summary>
    /// Copies an item along with its properties and metadata.
    /// </summary>
    /// <param name="source">Item to copy.</param>
    /// <returns>An independent copy of the item.</returns>
    public static IItem Copy(IItem source)
    {
        ArgumentNullException.ThrowIfNull(source);

        IItem copy = source switch {
            FreehandItem item => new FreehandItem {
                Points = CopyPoints(item.Points),
                StrokeColor = item.StrokeColor,
                StrokeWidth = item.StrokeWidth,
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
                SideLength = item.SideLength
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

        // Keep the original identity when restoring an item.
        copy.Id = source.Id;
        copy.Zindex = source.Zindex;
        copy.CreatedBy = source.CreatedBy;
        copy.CreatedDateTime = source.CreatedDateTime;
        copy.LastModifiedDateTime = source.LastModifiedDateTime;
        copy.Orientation = source.Orientation;

        if (source is Shape sourceShape && copy is Shape copiedShape)
        {
            copiedShape.FillColor = sourceShape.FillColor;
            copiedShape.BorderColor = sourceShape.BorderColor;
            copiedShape.BorderWidth = sourceShape.BorderWidth;
        }

        return copy;
    }

    /// <summary>
    /// Copies a point, or returns null if it is missing.
    /// </summary>
    private static Point2D CopyPoint(Point2D point)
    {
        return point is null
            ? null
            : new Point2D(point.X, point.Y);
    }

    /// <summary>
    /// Copies the list and its points so later edits do not affect history.
    /// </summary>
    private static List<Point2D> CopyPoints(List<Point2D> points)
    {
        return points?.Select(CopyPoint).ToList();
    }
}
