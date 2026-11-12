// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Interfaces.Shapes;
using Whiteboard.Interfaces;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Items;

public class BoundingBoxTests : BaseGenerator
{
    [Fact]
    public void Circle()

    {
        ICircle circle = new Whiteboard.Items.Shapes.Circle() {
            Center = RandomPointGenerator.Generate(),
            Radius = Rng.NextDouble() * 100
        };

        Assert.Equal(circle.Center.X - circle.Radius, circle.BoundingBox.Left);
        Assert.Equal(circle.Center.Y - circle.Radius, circle.BoundingBox.Top);
        Assert.Equal(circle.Radius * 2, circle.BoundingBox.Width);
        Assert.Equal(circle.Radius * 2, circle.BoundingBox.Height);
    }

    [Fact]
    public void CircleBoundingBoxSetter_UpdatesPositionAndUsesSmallerDimension()
    {
        Whiteboard.Items.Shapes.Circle circle = new() { Radius = 10 };

        circle.BoundingBox = new Rect2D(10, 20, 30, 40);

        Assert.Equal(25, circle.Left);
        Assert.Equal(40, circle.Top);
        Assert.Equal(15, circle.Radius);
    }

    [Fact]
    public void EquilateralTriangleBoundingBox_UsesSideLengthAndTriangleHeight()
    {
        Whiteboard.Items.Shapes.EqTriangle triangle = new() {
            Left = 10,
            Top = 20,
            SideLength = 30
        };

        Rect2D boundingBox = triangle.BoundingBox;

        Assert.Equal(10, boundingBox.Left);
        Assert.Equal(20, boundingBox.Top);
        Assert.Equal(30, boundingBox.Width);
        Assert.Equal(30 * Math.Sqrt(3) / 2, boundingBox.Height);
    }

    [Fact]
    public void Ellipse()
    {
        IEllipse ellipse = new Whiteboard.Items.Shapes.Ellipse() {
            Center = RandomPointGenerator.Generate(),
            Width = Rng.NextDouble() * 100,
            Height = Rng.NextDouble() * 100
        };

        Assert.Equal(ellipse.Center.X - (ellipse.Width / 2), ellipse.BoundingBox.Left);
        Assert.Equal(ellipse.Center.Y - (ellipse.Height / 2), ellipse.BoundingBox.Top);
        Assert.Equal(ellipse.Width, ellipse.BoundingBox.Width);
        Assert.Equal(ellipse.Height, ellipse.BoundingBox.Height);
    }
}
