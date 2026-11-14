using WhiteboardSuggestion.Core;
using Xunit;

namespace Insights.Test;

public sealed class ShapeSuggestionParserTests
{
    [Fact]
    public void Parse_Unknown_ReturnsUnknownResult()
    {
        ShapeSuggestionResult result = ShapeSuggestionParser.Parse(CreateResponse("Unknown"));

        Assert.IsType<ShapeSuggestionResult.Unknown>(result);
        Assert.Equal(0.8, result.Confidence);
    }

    [Fact]
    public void Parse_Line_ReturnsEndpoints()
    {
        const string json = """
        {
          "type": "Line",
          "confidence": 0.91,
          "startX": 12.5,
          "startY": 20,
          "endX": 180,
          "endY": 75,
          "vertices": [],
          "centerX": 0,
          "centerY": 0,
          "radius": 0,
          "radiusX": 0,
          "radiusY": 0
        }
        """;

        var result = Assert.IsType<ShapeSuggestionResult.Line>(ShapeSuggestionParser.Parse(json));

        Assert.Equal((12.5, 20.0), result.Start);
        Assert.Equal((180.0, 75.0), result.End);
        Assert.Equal(0.91, result.Confidence);
    }

    [Fact]
    public void Parse_Polygon_ReturnsVerticesAndRemovesRepeatedClosingPoint()
    {
        const string json = """
        {
          "type": "Polygon",
          "confidence": 0.94,
          "startX": 0,
          "startY": 0,
          "endX": 0,
          "endY": 0,
          "vertices": [
            { "x": 20, "y": 10 },
            { "x": 90, "y": 15 },
            { "x": 75, "y": 80 },
            { "x": 20, "y": 10 }
          ],
          "centerX": 0,
          "centerY": 0,
          "radius": 0,
          "radiusX": 0,
          "radiusY": 0
        }
        """;

        var result = Assert.IsType<ShapeSuggestionResult.Polygon>(ShapeSuggestionParser.Parse(json));

        Assert.Equal(3, result.Vertices.Count);
        Assert.Equal((20.0, 10.0), result.Vertices[0]);
        Assert.Equal((90.0, 15.0), result.Vertices[1]);
        Assert.Equal((75.0, 80.0), result.Vertices[2]);
    }

    [Fact]
    public void Parse_Circle_ReturnsCenterAndRadius()
    {
        const string json = """
        {
          "type": "Circle",
          "confidence": 0.95,
          "startX": 0,
          "startY": 0,
          "endX": 0,
          "endY": 0,
          "vertices": [],
          "centerX": 150,
          "centerY": 100,
          "radius": 48.5,
          "radiusX": 0,
          "radiusY": 0
        }
        """;

        var result = Assert.IsType<ShapeSuggestionResult.Circle>(ShapeSuggestionParser.Parse(json));

        Assert.Equal((150.0, 100.0), result.Center);
        Assert.Equal(48.5, result.Radius);
    }

    [Fact]
    public void Parse_Ellipse_ReturnsCenterAndBothRadii()
    {
        const string json = """
        {
          "type": "Ellipse",
          "confidence": 0.89,
          "startX": 0,
          "startY": 0,
          "endX": 0,
          "endY": 0,
          "vertices": [],
          "centerX": 150,
          "centerY": 100,
          "radius": 0,
          "radiusX": 80,
          "radiusY": 40
        }
        """;

        var result = Assert.IsType<ShapeSuggestionResult.Ellipse>(ShapeSuggestionParser.Parse(json));

        Assert.Equal((150.0, 100.0), result.Center);
        Assert.Equal(80.0, result.RadiusX);
        Assert.Equal(40.0, result.RadiusY);
    }

    [Fact]
    public void Parse_InvalidLineWithSameEndpoints_ReturnsUnknown()
    {
        const string json = """
        {
          "type": "Line",
          "confidence": 0.8,
          "startX": 20,
          "startY": 30,
          "endX": 20,
          "endY": 30,
          "vertices": [],
          "centerX": 0,
          "centerY": 0,
          "radius": 0,
          "radiusX": 0,
          "radiusY": 0
        }
        """;

        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse(json));
    }

    [Fact]
    public void Parse_CircleWithNonPositiveRadius_ReturnsUnknown()
    {
        string json = CreateResponse("Circle", radius: 0);

        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse(json));
    }

    [Fact]
    public void Parse_PolygonWithFewerThanThreeDistinctVertices_ReturnsUnknown()
    {
        string json = CreateResponse(
            "Polygon",
            vertices: "[{\"x\":1,\"y\":1},{\"x\":2,\"y\":2},{\"x\":1,\"y\":1}]");

        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse(json));
    }

    [Fact]
    public void Parse_UnsupportedResultType_ReturnsUnknown()
    {
        ShapeSuggestionResult result = ShapeSuggestionParser.Parse(CreateResponse("Pentagon"));

        Assert.IsType<ShapeSuggestionResult.Unknown>(result);
    }

    [Fact]
    public void Parse_ClampsConfidenceToRange()
    {
        ShapeSuggestionResult result = ShapeSuggestionParser.Parse(CreateResponse("Unknown", confidence: 1.5));

        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public void Parse_JsonInsideMarkdownFence_IsAccepted()
    {
        string json = CreateResponse("Unknown");
        string response = $"```json\n{json}\n```";

        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse(response));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    public void Parse_EmptyOrMissingJson_ThrowsFormatException(string response)
    {
        Assert.Throws<FormatException>(() => ShapeSuggestionParser.Parse(response));
    }

    private static string CreateResponse(
        string type,
        double confidence = 0.8,
        double radius = 0,
        string vertices = "[]")
    {
        return $$"""
        {
          "type": "{{type}}",
          "confidence": {{confidence}},
          "startX": 0,
          "startY": 0,
          "endX": 1,
          "endY": 1,
          "vertices": {{vertices}},
          "centerX": 10,
          "centerY": 10,
          "radius": {{radius}},
          "radiusX": 5,
          "radiusY": 4
        }
        """;
    }
}
