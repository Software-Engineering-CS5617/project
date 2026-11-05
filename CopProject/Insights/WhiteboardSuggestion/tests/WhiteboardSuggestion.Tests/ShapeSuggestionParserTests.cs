using WhiteboardSuggestion.Core;
using Xunit;

namespace WhiteboardSuggestion.Tests;

public sealed class ShapeSuggestionParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsShapeAndAlternatives()
    {
        const string json = """
        {
          "category": "Shape",
          "bestMatch": "Circle",
          "alternatives": ["Oval", "Ellipse"],
          "confidence": 0.87,
          "explanation": "The drawing is almost round."
        }
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(json);

        Assert.Equal(DrawingCategory.Shape, result.Category);
        Assert.Equal("Circle", result.BestMatch);
        Assert.Equal(new[] { "Oval", "Ellipse" }, result.Alternatives);
        Assert.Equal(0.87, result.Confidence, 2);
    }

    [Fact]
    public void Parse_JsonInsideMarkdownFence_StillParses()
    {
        const string response = """
        ```json
        {"category":"Letter","bestMatch":"Letter M","alternatives":["Letter N"],"confidence":0.74,"explanation":"Two peaks are visible."}
        ```
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(response);

        Assert.Equal(DrawingCategory.Letter, result.Category);
        Assert.Equal("Letter M", result.BestMatch);
    }

    [Fact]
    public void Parse_UnknownCategory_UsesUnknown()
    {
        const string json = """
        {"category":"Animal","bestMatch":"Cat","alternatives":[],"confidence":0.5,"explanation":"Unexpected category."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(json);

        Assert.Equal(DrawingCategory.Unknown, result.Category);
    }

    [Fact]
    public void Parse_ClampsConfidenceAboveOne()
    {
        const string json = """
        {"category":"Digit","bestMatch":"Digit 8","alternatives":[],"confidence":1.7,"explanation":""}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(json);

        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public void Parse_ClampsConfidenceBelowZero()
    {
        const string json = """
        {"category":"Digit","bestMatch":"Digit 3","alternatives":[],"confidence":-0.4,"explanation":""}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(json);

        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public void Parse_RemovesDuplicateAndBestMatchAlternatives()
    {
        const string json = """
        {"category":"Shape","bestMatch":"Circle","alternatives":["Circle","Oval","oval","Ellipse"],"confidence":0.8,"explanation":""}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(json);

        Assert.Equal(new[] { "Oval", "Ellipse" }, result.Alternatives);
    }

    [Fact]
    public void Parse_EmptyBestMatch_FallsBackToUnclearAndUnknown()
    {
        const string json = """
        {"category":"Shape","bestMatch":"  ","alternatives":[],"confidence":0.1,"explanation":"Not enough information."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(json);

        Assert.Equal(DrawingCategory.Unknown, result.Category);
        Assert.Equal("Unclear", result.BestMatch);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a JSON response")]
    public void Parse_InvalidResponse_ThrowsFormatException(string response)
    {
        Assert.Throws<FormatException>(() => ShapeSuggestionParser.Parse(response));
    }

    [Fact]
    public void CleanPlainText_RemovesMarkdownDecoration()
    {
        string result = ShapeSuggestionParser.CleanPlainText("**## Circle**");

        Assert.Equal("Circle", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("#", result);
        Assert.DoesNotContain("`", result);
    }
}
