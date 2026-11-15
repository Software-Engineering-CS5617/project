/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Services;
using WhiteboardSuggestion.Core;
using Windows.UI;
using Xunit;

namespace Whiteboard.Test.Services;

public class ShapeRecognitionServiceTests
{
    private class MockShapeSuggestionService : IShapeSuggestionService
    {
        public ShapeSuggestionResult? ResultToReturn { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public StrokeRecognitionRequest? LastRequest { get; private set; }

        public Task<ShapeSuggestionResult> SuggestShapeAsync(StrokeRecognitionRequest request)
        {
            LastRequest = request;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(ResultToReturn ?? new ShapeSuggestionResult.Unknown(0.0));
        }
    }

    private static FreehandItem CreateSampleFreehand(int pointCount = 5)
    {
        List<Point2D> points = [];
        for (int i = 0; i < pointCount; i++)
        {
            points.Add(new Point2D(i * 10, i * 10));
        }

        return new FreehandItem
        {
            Points = points,
            StrokeColor = Color.FromArgb(255, 12, 34, 56),
            StrokeWidth = 4,
            CreatedBy = "test-user",
            Zindex = 7
        };
    }

    [Fact]
    public void ConvertResultToItem_Line_MapsPropertiesCorrectly()
    {
        FreehandItem freehand = CreateSampleFreehand();
        ShapeSuggestionResult.Line lineResult = new(new(10, 20), new(100, 200), 0.95);

        IItem? item = ShapeRecognitionService.ConvertResultToItem(lineResult, freehand);

        Assert.NotNull(item);
        Line line = Assert.IsType<Line>(item);
        Assert.Equal(10, line.Begin.X);
        Assert.Equal(20, line.Begin.Y);
        Assert.Equal(100, line.End.X);
        Assert.Equal(200, line.End.Y);
        Assert.Equal(freehand.StrokeColor, line.Color);
        Assert.Equal(freehand.StrokeWidth, line.Width);
        Assert.Equal(freehand.CreatedBy, line.CreatedBy);
        Assert.Equal(freehand.Zindex, line.Zindex);
    }

    [Fact]
    public void ConvertResultToItem_Circle_MapsPropertiesCorrectly()
    {
        FreehandItem freehand = CreateSampleFreehand();
        ShapeSuggestionResult.Circle circleResult = new(new(50, 60), 25, 0.9);

        IItem? item = ShapeRecognitionService.ConvertResultToItem(circleResult, freehand);

        Assert.NotNull(item);
        Circle circle = Assert.IsType<Circle>(item);
        Assert.Equal(50, circle.Center.X);
        Assert.Equal(60, circle.Center.Y);
        Assert.Equal(25, circle.Radius);
        Assert.Equal(freehand.StrokeColor, circle.BorderColor);
        Assert.Equal(freehand.StrokeWidth, circle.BorderWidth);
        Assert.Equal(freehand.CreatedBy, circle.CreatedBy);
        Assert.Equal(freehand.Zindex, circle.Zindex);
    }

    [Fact]
    public void ConvertResultToItem_Ellipse_MapsPropertiesCorrectly()
    {
        FreehandItem freehand = CreateSampleFreehand();
        ShapeSuggestionResult.Ellipse ellipseResult = new(new(50, 60), 30, 15, 0.88);

        IItem? item = ShapeRecognitionService.ConvertResultToItem(ellipseResult, freehand);

        Assert.NotNull(item);
        Ellipse ellipse = Assert.IsType<Ellipse>(item);
        Assert.Equal(50, ellipse.Center.X);
        Assert.Equal(60, ellipse.Center.Y);
        Assert.Equal(60, ellipse.Width);  // RadiusX * 2
        Assert.Equal(30, ellipse.Height); // RadiusY * 2
        Assert.Equal(freehand.StrokeColor, ellipse.BorderColor);
        Assert.Equal(freehand.StrokeWidth, ellipse.BorderWidth);
        Assert.Equal(freehand.CreatedBy, ellipse.CreatedBy);
        Assert.Equal(freehand.Zindex, ellipse.Zindex);
    }

    [Fact]
    public void ConvertResultToItem_Polygon_MapsPropertiesCorrectly()
    {
        FreehandItem freehand = CreateSampleFreehand();
        ShapeSuggestionResult.Polygon polygonResult = new(
            [new(0, 0), new(50, 100), new(100, 0)],
            0.85);

        IItem? item = ShapeRecognitionService.ConvertResultToItem(polygonResult, freehand);

        Assert.NotNull(item);
        Polygon polygon = Assert.IsType<Polygon>(item);
        Assert.Equal(3, polygon.Vertices.Count);
        Assert.Equal(0, polygon.Vertices[0].X);
        Assert.Equal(0, polygon.Vertices[0].Y);
        Assert.Equal(50, polygon.Vertices[1].X);
        Assert.Equal(100, polygon.Vertices[1].Y);
        Assert.Equal(100, polygon.Vertices[2].X);
        Assert.Equal(0, polygon.Vertices[2].Y);
        Assert.Equal(freehand.StrokeColor, polygon.BorderColor);
        Assert.Equal(freehand.StrokeWidth, polygon.BorderWidth);
        Assert.Equal(freehand.CreatedBy, polygon.CreatedBy);
        Assert.Equal(freehand.Zindex, polygon.Zindex);
    }

    [Fact]
    public void ConvertResultToItem_PolygonWithFewerThan3Vertices_ReturnsNull()
    {
        FreehandItem freehand = CreateSampleFreehand();
        ShapeSuggestionResult.Polygon polygonResult = new([new(0, 0), new(10, 10)], 0.9);

        IItem? item = ShapeRecognitionService.ConvertResultToItem(polygonResult, freehand);
        Assert.Null(item);
    }

    [Fact]
    public void ConvertResultToItem_Unknown_ReturnsNull()
    {
        FreehandItem freehand = CreateSampleFreehand();
        ShapeSuggestionResult.Unknown unknownResult = new(0.3);

        IItem? item = ShapeRecognitionService.ConvertResultToItem(unknownResult, freehand);
        Assert.Null(item);
    }

    [Fact]
    public async Task TryRecognizeShapeAsync_WhenDisabled_ReturnsNull()
    {
        MockShapeSuggestionService mockService = new()
        {
            ResultToReturn = new ShapeSuggestionResult.Circle(new(50, 50), 20, 0.99)
        };
        ShapeRecognitionService service = new(mockService) { IsEnabled = false };

        IItem? result = await service.TryRecognizeShapeAsync(CreateSampleFreehand());
        Assert.Null(result);
    }

    [Fact]
    public async Task TryRecognizeShapeAsync_WhenNoService_ReturnsNull()
    {
        ShapeRecognitionService service = new(suggestionService: null) { IsEnabled = true };

        IItem? result = await service.TryRecognizeShapeAsync(CreateSampleFreehand());
        Assert.Null(result);
    }

    [Fact]
    public async Task TryRecognizeShapeAsync_WhenPointsLessThan2_ReturnsNull()
    {
        MockShapeSuggestionService mockService = new()
        {
            ResultToReturn = new ShapeSuggestionResult.Circle(new(50, 50), 20, 0.99)
        };
        ShapeRecognitionService service = new(mockService) { IsEnabled = true };

        FreehandItem freehand = new() { Points = [new(10, 10)] };
        IItem? result = await service.TryRecognizeShapeAsync(freehand);
        Assert.Null(result);
    }

    [Fact]
    public async Task TryRecognizeShapeAsync_WhenConfidenceBelowThreshold_ReturnsNull()
    {
        MockShapeSuggestionService mockService = new()
        {
            ResultToReturn = new ShapeSuggestionResult.Circle(new(50, 50), 20, 0.50) // below default 0.65
        };
        ShapeRecognitionService service = new(mockService)
        {
            IsEnabled = true,
            ConfidenceThreshold = 0.65
        };

        IItem? result = await service.TryRecognizeShapeAsync(CreateSampleFreehand());
        Assert.Null(result);
    }

    [Fact]
    public async Task TryRecognizeShapeAsync_WhenConfidenceAboveThreshold_ReturnsItem()
    {
        MockShapeSuggestionService mockService = new()
        {
            ResultToReturn = new ShapeSuggestionResult.Circle(new(50, 50), 20, 0.85)
        };
        ShapeRecognitionService service = new(mockService)
        {
            IsEnabled = true,
            ConfidenceThreshold = 0.65
        };

        IItem? result = await service.TryRecognizeShapeAsync(CreateSampleFreehand());
        Assert.NotNull(result);
        Assert.IsType<Circle>(result);
    }

    [Fact]
    public async Task TryRecognizeShapeAsync_WhenServiceThrows_ReturnsNullGracefully()
    {
        MockShapeSuggestionService mockService = new()
        {
            ExceptionToThrow = new InvalidOperationException("API Key invalid or network down")
        };
        ShapeRecognitionService service = new(mockService) { IsEnabled = true };

        IItem? result = await service.TryRecognizeShapeAsync(CreateSampleFreehand());
        Assert.Null(result);
    }
}
