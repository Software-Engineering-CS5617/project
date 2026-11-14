/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;

namespace Whiteboard.Test.Items;

public class MoveByTests
{
    [Fact]
    public void Circle_MoveBy_UpdatesCenterAndBoundingBox()
    {
        Circle circle = new() {
            Center = new Point2D(100, 100),
            Radius = 25
        };

        circle.MoveBy(20, -30);

        Assert.Equal(120, circle.Center.X);
        Assert.Equal(70, circle.Center.Y);
        Assert.Equal(95, circle.Left);
        Assert.Equal(45, circle.Top);
        Assert.Equal(50, circle.BoundingBox.Width);
        Assert.Equal(50, circle.BoundingBox.Height);
    }

    [Fact]
    public void Square_MoveBy_UpdatesCenterAndBoundingBox()
    {
        Square square = new() {
            Center = new Point2D(50, 50),
            SideLegth = 40
        };

        square.MoveBy(15, 25);

        Assert.Equal(65, square.Center.X);
        Assert.Equal(75, square.Center.Y);
        Assert.Equal(45, square.Left);
        Assert.Equal(55, square.Top);
    }

    [Fact]
    public void Rectangle_MoveBy_UpdatesCenterAndBoundingBox()
    {
        Rectangle rectangle = new() {
            Center = new Point2D(200, 150),
            Width = 100,
            Height = 60
        };

        rectangle.MoveBy(-50, 30);

        Assert.Equal(150, rectangle.Center.X);
        Assert.Equal(180, rectangle.Center.Y);
        Assert.Equal(100, rectangle.Left);
        Assert.Equal(150, rectangle.Top);
    }

    [Fact]
    public void Ellipse_MoveBy_UpdatesCenterAndBoundingBox()
    {
        Ellipse ellipse = new() {
            Center = new Point2D(80, 90),
            Width = 60,
            Height = 40
        };

        ellipse.MoveBy(10, -10);

        Assert.Equal(90, ellipse.Center.X);
        Assert.Equal(80, ellipse.Center.Y);
        Assert.Equal(60, ellipse.Left);
        Assert.Equal(60, ellipse.Top);
    }

    [Fact]
    public void EqTriangle_MoveBy_UpdatesCenterAndBoundingBox()
    {
        EqTriangle triangle = new() {
            Center = new Point2D(100, 100),
            SideLength = 50
        };

        double originalLeft = triangle.Left;
        double originalTop = triangle.Top;

        triangle.MoveBy(30, 40);

        Assert.Equal(130, triangle.Center.X);
        Assert.Equal(140, triangle.Center.Y);
        Assert.Equal(originalLeft + 30, triangle.Left);
        Assert.Equal(originalTop + 40, triangle.Top);
    }

    [Fact]
    public void Line_MoveBy_UpdatesBeginEndAndBoundingBox()
    {
        Line line = new() {
            Begin = new Point2D(10, 20),
            End = new Point2D(50, 80)
        };

        line.MoveBy(15, -10);

        Assert.Equal(25, line.Begin.X);
        Assert.Equal(10, line.Begin.Y);
        Assert.Equal(65, line.End.X);
        Assert.Equal(70, line.End.Y);
        Assert.Equal(25, line.Left);
        Assert.Equal(10, line.Top);
    }

    [Fact]
    public void Polygon_MoveBy_UpdatesVerticesAndBoundingBox()
    {
        Polygon polygon = new() {
            Vertices = new List<Point2D> {
                new(0, 0),
                new(100, 0),
                new(50, 80)
            }
        };

        polygon.MoveBy(25, 35);

        Assert.Equal(25, polygon.Vertices[0].X);
        Assert.Equal(35, polygon.Vertices[0].Y);
        Assert.Equal(125, polygon.Vertices[1].X);
        Assert.Equal(35, polygon.Vertices[1].Y);
        Assert.Equal(75, polygon.Vertices[2].X);
        Assert.Equal(115, polygon.Vertices[2].Y);
        Assert.Equal(25, polygon.Left);
        Assert.Equal(35, polygon.Top);
    }

    [Fact]
    public void Freehand_MoveBy_UpdatesPointsAndBoundingBox()
    {
        FreehandItem freehand = new() {
            Points = new List<Point2D> {
                new(10, 20),
                new(30, 40),
                new(60, 80)
            }
        };

        freehand.MoveBy(10, -5);

        Assert.Equal(20, freehand.Points[0].X);
        Assert.Equal(15, freehand.Points[0].Y);
        Assert.Equal(70, freehand.Points[2].X);
        Assert.Equal(75, freehand.Points[2].Y);
        Assert.Equal(20, freehand.Left);
        Assert.Equal(15, freehand.Top);
    }

    [Fact]
    public void Textbox_MoveBy_UpdatesCenterAndBoundingBox()
    {
        Textbox textbox = new() {
            Center = new Point2D(100, 60),
            Width = 80,
            Height = 30
        };

        textbox.MoveBy(20, 15);

        Assert.Equal(120, textbox.Center.X);
        Assert.Equal(75, textbox.Center.Y);
        Assert.Equal(80, textbox.Left);
        Assert.Equal(60, textbox.Top);
    }
}
