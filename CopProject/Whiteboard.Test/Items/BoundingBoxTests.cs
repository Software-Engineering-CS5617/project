// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Interfaces;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Items;

public class BoundingBoxTests : BaseGenerator
{
    /// <summary>
    /// Test bounding box for circle is calculated properly
    /// </summary>
    [Fact]
    public void Circle()
    {
        var circle = new Whiteboard.Items.Shapes.Circle() {
            Center = RandomPointGenerator.Generate(),
            Radius = Rng.NextDouble() * 100
        };

        Assert.Equal(circle.Center.X - circle.Radius, circle.BoundingBox.Left);
        Assert.Equal(circle.Center.Y - circle.Radius, circle.BoundingBox.Top);
        Assert.Equal(circle.Radius * 2, circle.BoundingBox.Width);
        Assert.Equal(circle.Radius * 2, circle.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for ellipse is calculated properly
    /// </summary>
    [Fact]
    public void CircleBoundingBox_CalculatesFromCenterAndRadius()
    {
        Whiteboard.Items.Shapes.Circle circle = new() {
            Center = new Point2D(40, 55),
            Radius = 15
        };

        Assert.Equal(25, circle.Left);
        Assert.Equal(40, circle.Top);
        Assert.Equal(15, circle.Radius);
    }

    [Fact]
    public void Ellipse()
    {
        var ellipse = new Whiteboard.Items.Shapes.Ellipse() {
            Center = RandomPointGenerator.Generate(),
            Width = Rng.NextDouble() * 100,
            Height = Rng.NextDouble() * 100
        };

        Assert.Equal(ellipse.Center.X - (ellipse.Width / 2), ellipse.BoundingBox.Left);
        Assert.Equal(ellipse.Center.Y - (ellipse.Height / 2), ellipse.BoundingBox.Top);
        Assert.Equal(ellipse.Width, ellipse.BoundingBox.Width);
        Assert.Equal(ellipse.Height, ellipse.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for equilateral triangle is calculated properly
    /// </summary>
    [Fact]
    public void EqTriangle()
    {
        var eqTriangle = new Whiteboard.Items.Shapes.EqTriangle() {
            Center = RandomPointGenerator.Generate(),
            SideLength = Rng.NextDouble() * 100
        };

        double height = (Math.Sqrt(3.0) / 2.0) * eqTriangle.SideLength;

        Assert.Equal(eqTriangle.Center.X - (eqTriangle.SideLength / 2), eqTriangle.BoundingBox.Left);
        Assert.Equal(eqTriangle.Center.Y - ((2.0 / 3.0) * height), eqTriangle.BoundingBox.Top);
        Assert.Equal(eqTriangle.SideLength, eqTriangle.BoundingBox.Width);
        Assert.Equal(height, eqTriangle.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for square is calculated properly
    /// </summary>
    [Fact]
    public void Square()
    {
        var square = new Whiteboard.Items.Shapes.Square() {
            Center = RandomPointGenerator.Generate(),
            SideLegth = Rng.NextDouble() * 100
        };

        Assert.Equal(square.Center.X - (square.SideLegth / 2), square.BoundingBox.Left);
        Assert.Equal(square.Center.Y - (square.SideLegth / 2), square.BoundingBox.Top);
        Assert.Equal(square.SideLegth, square.BoundingBox.Width);
        Assert.Equal(square.SideLegth, square.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for rectangle is calculated properly
    /// </summary>
    [Fact]
    public void Rectangle()
    {
        var rectangle = new Whiteboard.Items.Shapes.Rectangle() {
            Center = RandomPointGenerator.Generate(),
            Width = Rng.NextDouble() * 100,
            Height = Rng.NextDouble() * 100
        };

        Assert.Equal(rectangle.Center.X - (rectangle.Width / 2), rectangle.BoundingBox.Left);
        Assert.Equal(rectangle.Center.Y - (rectangle.Height / 2), rectangle.BoundingBox.Top);
        Assert.Equal(rectangle.Width, rectangle.BoundingBox.Width);
        Assert.Equal(rectangle.Height, rectangle.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for polygon is calculated properly
    /// </summary>
    [Fact]
    public void Polygon()
    {
        var polygon = new Whiteboard.Items.Shapes.Polygon() {
            Vertices = RandomPointListGenerator.Generate()
        };

        double minX = polygon.Vertices.Min(p => p.X);
        double maxX = polygon.Vertices.Max(p => p.X);
        double minY = polygon.Vertices.Min(p => p.Y);
        double maxY = polygon.Vertices.Max(p => p.Y);

        Assert.Equal(minX, polygon.BoundingBox.Left);
        Assert.Equal(minY, polygon.BoundingBox.Top);
        Assert.Equal(maxX - minX, polygon.BoundingBox.Width);
        Assert.Equal(maxY - minY, polygon.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for line is calculated properly
    /// </summary>
    [Fact]
    public void Line()
    {
        var line = new Whiteboard.Items.Line() {
            Begin = RandomPointGenerator.Generate(),
            End = RandomPointGenerator.Generate()
        };

        double left = Math.Min(line.Begin.X, line.End.X);
        double top = Math.Min(line.Begin.Y, line.End.Y);
        double right = Math.Max(line.Begin.X, line.End.X);
        double bottom = Math.Max(line.Begin.Y, line.End.Y);

        double width = right - left;
        double height = bottom - top;

        Assert.Equal(left, line.BoundingBox.Left);
        Assert.Equal(top, line.BoundingBox.Top);
        Assert.Equal(width, line.BoundingBox.Width);
        Assert.Equal(height, line.BoundingBox.Height);
    }
}
