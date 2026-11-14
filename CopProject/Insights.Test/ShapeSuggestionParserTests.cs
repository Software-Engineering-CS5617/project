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
        string json = CreateResponse("Line", startX: 10, startY: 20, endX: 30, endY: 40);
        var result = Assert.IsType<ShapeSuggestionResult.Line>(ShapeSuggestionParser.Parse(json));
        Assert.Equal((10.0, 20.0), result.Start);
        Assert.Equal((30.0, 40.0), result.End);
    }

    [Fact]
    public void Parse_Polygon_RemovesConsecutiveAndClosingDuplicates()
    {
        const string vertices = """[{"x":0,"y":0},{"x":0,"y":0},{"x":10,"y":0},{"x":10,"y":10},{"x":0,"y":10},{"x":0,"y":0}]""";
        var result = Assert.IsType<ShapeSuggestionResult.Polygon>(
            ShapeSuggestionParser.Parse(CreateResponse("Polygon", vertices: vertices)));
        Assert.Equal(new[] { (0.0, 0.0), (10.0, 0.0), (10.0, 10.0), (0.0, 10.0) }, result.Vertices);
    }

    [Fact]
    public void Parse_Circle_ReturnsCenterAndRadius()
    {
        var result = Assert.IsType<ShapeSuggestionResult.Circle>(
            ShapeSuggestionParser.Parse(CreateResponse("Circle", centerX: 100, centerY: 80, radius: 25)));
        Assert.Equal((100.0, 80.0), result.Center);
        Assert.Equal(25.0, result.Radius);
    }

    [Fact]
    public void Parse_Ellipse_ReturnsCenterAndRadii()
    {
        var result = Assert.IsType<ShapeSuggestionResult.Ellipse>(
            ShapeSuggestionParser.Parse(CreateResponse("Ellipse", centerX: 100, centerY: 80, radiusX: 40, radiusY: 20)));
        Assert.Equal((100.0, 80.0), result.Center);
        Assert.Equal(40.0, result.RadiusX);
        Assert.Equal(20.0, result.RadiusY);
    }

    [Fact]
    public void Parse_LineWithIdenticalEndpoints_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(
            ShapeSuggestionParser.Parse(CreateResponse("Line", startX: 5, startY: 5, endX: 5, endY: 5)));
    }

    [Fact]
    public void Parse_LineWithMissingInvalidOrNonFiniteCoordinate_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Line","startY":1,"endX":2,"endY":3}"""));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Line","startX":"bad","startY":1,"endX":2,"endY":3}"""));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Line","startX":1e400,"startY":1,"endX":2,"endY":3}"""));
    }

    [Fact]
    public void Parse_PolygonWithMissingOrNonArrayVertices_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Polygon"}"""));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Polygon","vertices":{}}"""));
    }

    [Fact]
    public void Parse_PolygonWithInvalidVertex_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse(CreateResponse("Polygon", vertices: "[null]")));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse(CreateResponse("Polygon", vertices: "[{\"x\":1}]")));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse(CreateResponse("Polygon", vertices: "[{\"x\":\"bad\",\"y\":2}]")));
    }

    [Fact]
    public void Parse_PolygonWithFewerThanThreeDistinctVertices_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(
            ShapeSuggestionParser.Parse(CreateResponse("Polygon", vertices: "[{\"x\":1,\"y\":1},{\"x\":2,\"y\":2},{\"x\":1,\"y\":1}]")));
    }

    [Fact]
    public void Parse_UnsupportedType_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse(CreateResponse("Pentagon")));
    }

    [Fact]
    public void Parse_MissingType_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"confidence":0.7}"""));
    }

    [Fact]
    public void Parse_NonStringType_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":123,"confidence":0.7}"""));
    }

    [Fact]
    public void Parse_MissingOrInvalidConfidence_DefaultsToZero()
    {
        Assert.Equal(0, ShapeSuggestionParser.Parse("""{"type":"Unknown"}""").Confidence);
        Assert.Equal(0, ShapeSuggestionParser.Parse("""{"type":"Unknown","confidence":"high"}""").Confidence);
        Assert.Equal(0, ShapeSuggestionParser.Parse("""{"type":"Unknown","confidence":1e400}""").Confidence);
    }

    [Fact]
    public void Parse_ClampsConfidenceToZeroThroughOne()
    {
        Assert.Equal(1, ShapeSuggestionParser.Parse(CreateResponse("Unknown", confidence: 1.5)).Confidence);
        Assert.Equal(0, ShapeSuggestionParser.Parse(CreateResponse("Unknown", confidence: -0.5)).Confidence);
    }

    [Fact]
    public void Parse_InvalidCircleGeometry_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Circle","centerX":1,"centerY":2,"radius":0}"""));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Circle","centerX":1,"centerY":2,"radius":-2}"""));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Circle","centerX":"bad","centerY":2,"radius":2}"""));
    }

    [Fact]
    public void Parse_InvalidEllipseGeometry_ReturnsUnknown()
    {
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Ellipse","centerY":2,"radiusX":5,"radiusY":4}"""));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Ellipse","centerX":1,"centerY":2,"radiusX":0,"radiusY":4}"""));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse("""{"type":"Ellipse","centerX":1,"centerY":2,"radiusX":5,"radiusY":-4}"""));
    }

    [Fact]
    public void Parse_AcceptsMarkdownFenceAndSurroundingText()
    {
        string json = CreateResponse("Unknown");
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse($"```json\n{json}\n```"));
        Assert.IsType<ShapeSuggestionResult.Unknown>(ShapeSuggestionParser.Parse($"Gemini response: {json} end"));
    }

    [Fact]
    public void Parse_EmptyOrMalformedResponse_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ShapeSuggestionParser.Parse(null!));
        Assert.Throws<FormatException>(() => ShapeSuggestionParser.Parse("   "));
        Assert.Throws<FormatException>(() => ShapeSuggestionParser.Parse("not json"));
        Assert.Throws<FormatException>(() => ShapeSuggestionParser.Parse("{ invalid json }"));
    }

    [Fact]
    public void Parse_RecognizesTypeWithoutCaseOrWhitespaceSensitivity()
    {
        Assert.IsType<ShapeSuggestionResult.Ellipse>(
            ShapeSuggestionParser.Parse(CreateResponse("  eLLiPsE  ", centerX: 10, centerY: 10, radiusX: 4, radiusY: 3)));
    }

    private static string CreateResponse(
        string type,
        double confidence = 0.8,
        double startX = 0,
        double startY = 0,
        double endX = 1,
        double endY = 1,
        string vertices = "[]",
        double centerX = 10,
        double centerY = 10,
        double radius = 5,
        double radiusX = 5,
        double radiusY = 4)
    {
        return $$"""
        {
          "type": "{{type}}",
          "confidence": {{confidence}},
          "startX": {{startX}}, "startY": {{startY}},
          "endX": {{endX}}, "endY": {{endY}},
          "vertices": {{vertices}},
          "centerX": {{centerX}}, "centerY": {{centerY}},
          "radius": {{radius}}, "radiusX": {{radiusX}}, "radiusY": {{radiusY}}
        }
        """;
    }
}
