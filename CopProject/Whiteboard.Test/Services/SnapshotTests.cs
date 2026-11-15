// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/****
    * Filename  : SnapshotTests.cs
    * Owner     : Sangam
    * Roll No   : 112201006
****/


using System;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Services;
using Windows.UI;
using Xunit;

namespace Whiteboard.Test.Services;

/// <summary>
/// Tests item copies, saved properties, and independent drawing points.
/// </summary>
public class SnapshotTests
{
    /// <summary>
    /// Checks that a square copy preserves its properties and calculates its own bounds.
    /// </summary>
    [Fact]
    public void SquareCopyPreservesProperties()
    {
        var original = new Square {
            Center = new Point2D(100, 100),
            SideLegth = 40,
            FillColor = Color.FromArgb(255, 255, 0, 0),
            BorderColor = Color.FromArgb(255, 0, 0, 255),
            BorderWidth = 3,
            CreatedBy = "Host",
            Zindex = 2,
            Orientation = 30
        };

        var copy = Assert.IsType<Square>(ItemSnapshot.Copy(original));

        Assert.NotSame(original, copy);
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.CreatedBy, copy.CreatedBy);
        Assert.Equal(original.CreatedDateTime, copy.CreatedDateTime);
        Assert.Equal(original.LastModifiedDateTime, copy.LastModifiedDateTime);
        Assert.Equal(original.Center, copy.Center);
        Assert.Equal(original.SideLegth, copy.SideLegth);
        Assert.Equal(original.FillColor, copy.FillColor);
        Assert.Equal(original.BorderColor, copy.BorderColor);
        Assert.Equal(original.BorderWidth, copy.BorderWidth);
        Assert.Equal(original.Zindex, copy.Zindex);
        Assert.Equal(original.Orientation, copy.Orientation);

        // Bounds should follow the copied geometry.
        copy.SideLegth = 80;
        Assert.Equal(80, copy.BoundingBox.Width);
        Assert.Equal(40, original.BoundingBox.Width);
    }

    /// <summary>
    /// Checks that a stroke copy keeps its points and style when the original points change.
    /// </summary>
    [Fact]
    public void StrokeCopyHasIndependentPoints()
    {
        var original = new FreehandItem {
            Points = [new Point2D(10, 20), new Point2D(30, 40)],
            StrokeColor = Color.FromArgb(255, 0, 0, 255),
            StrokeWidth = 5
        };

        var copy = Assert.IsType<FreehandItem>(ItemSnapshot.Copy(original));
        original.Points[0] = new Point2D(999, 999);
        original.Points.Add(new Point2D(50, 60));

        Assert.NotSame(original.Points, copy.Points);
        Assert.Equal(2, copy.Points.Count);
        Assert.Equal(new Point2D(10, 20), copy.Points[0]);
        Assert.Equal(original.StrokeColor, copy.StrokeColor);
        Assert.Equal(5, copy.StrokeWidth);
    }

    /// <summary>
    /// Checks that a text copy preserves its content, position, size, and font settings.
    /// </summary>
    [Fact]
    public void TextCopyPreservesContentAndSize()
    {
        var original = new Textbox {
            Content = "Whiteboard",
            Center = new Point2D(100, 50),
            Width = 200,
            Height = 60,
            FontFamily = "Arial",
            FontSize = 24
        };

        var copy = Assert.IsType<Textbox>(ItemSnapshot.Copy(original));

        Assert.Equal(original.Content, copy.Content);
        Assert.Equal(original.Center, copy.Center);
        Assert.Equal(original.Width, copy.Width);
        Assert.Equal(original.Height, copy.Height);
        Assert.Equal(original.FontFamily, copy.FontFamily);
        Assert.Equal(original.FontSize, copy.FontSize);
        Assert.Equal(original.FontColor, copy.FontColor);
    }

    /// <summary>
    /// Checks that copying a null item is rejected.
    /// </summary>
    [Fact]
    public void NullItemIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => ItemSnapshot.Copy(null!));
    }
}

