using WhiteboardSuggestion.Core;
using Xunit;

namespace WhiteboardSuggestion.Tests;

public sealed class ShapeSuggestionParserTests
{
    [Fact]
    public void Parse_ValidShape_ReturnsTypedShapeAndAlternatives()
    {
        const string Json = """
        {
          "category": "Shape",
          "bestMatch": "Circle",
          "recognizedText": "",
          "alternatives": ["Ellipse", "Rectangle"],
          "confidence": 0.87,
          "explanation": "The drawing is almost round."
        }
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Shape, result.Category);
        Assert.Equal(ShapeType.Circle, result.BestMatch);
        Assert.Null(result.RecognizedText);
        Assert.Equal(2, result.Alternatives.Count);
        Assert.Equal(ShapeType.Ellipse, result.Alternatives[0].Shape);
        Assert.Equal(ShapeType.Rectangle, result.Alternatives[1].Shape);
        Assert.All(result.Alternatives, item => Assert.False(item.IsRecommended));
        Assert.Equal(0.87, result.Confidence, 2);
    }

    [Fact]
    public void Parse_EquilateralTriangle_MapsCanonicalNameToEnum()
    {
        const string Json = """
        {
          "category": "Shape",
          "bestMatch": "Equilateral Triangle",
          "recognizedText": "",
          "alternatives": [],
          "confidence": 0.91,
          "explanation": "The three sides appear similar in length."
        }
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(ShapeType.EquilateralTriangle, result.BestMatch);
    }

    [Theory]
    [InlineData("Square", ShapeType.Square)]
    [InlineData("Circle", ShapeType.Circle)]
    [InlineData("Rectangle", ShapeType.Rectangle)]
    [InlineData("Ellipse", ShapeType.Ellipse)]
    [InlineData("Equilateral Triangle", ShapeType.EquilateralTriangle)]
    [InlineData("Line", ShapeType.Line)]
    public void Parse_AllSupportedCanonicalShapeNames_ReturnEnum(string value, ShapeType expected)
    {
        string json = $$"""
        {
          "category": "Shape",
          "bestMatch": "{{value}}",
          "recognizedText": "",
          "alternatives": [],
          "confidence": 0.8,
          "explanation": "Supported shape."
        }
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(json);

        Assert.Equal(DrawingCategory.Shape, result.Category);
        Assert.Equal(expected, result.BestMatch);
    }

    [Theory]
    [InlineData("Oval", ShapeType.Ellipse)]
    [InlineData("Arrow", ShapeType.Line)]
    [InlineData("Line segment", ShapeType.Line)]
    public void Parse_RecognizedShapeAliases_MapToSupportedEnum(string value, ShapeType expected)
    {
        string json = $$"""
        {
          "category": "Shape",
          "bestMatch": "{{value}}",
          "recognizedText": "",
          "alternatives": [],
          "confidence": 0.8,
          "explanation": "Recognized shape."
        }
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(json);

        Assert.Equal(DrawingCategory.Shape, result.Category);
        Assert.Equal(expected, result.BestMatch);
    }

    [Fact]
    public void Parse_UnsupportedShape_ReturnsUnknownInsteadOfUnsupportedShape()
    {
        const string Json = """
        {
          "category": "Shape",
          "bestMatch": "Pentagon",
          "recognizedText": "",
          "alternatives": ["Hexagon"],
          "confidence": 0.9,
          "explanation": "The drawing has five sides."
        }
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Unknown, result.Category);
        Assert.Equal(ShapeType.Unknown, result.BestMatch);
        Assert.Null(result.RecognizedText);
        Assert.Empty(result.Alternatives);
    }

    [Fact]
    public void Parse_InvalidShapeValueInShapeCategory_ReturnsUnknown()
    {
        const string Json = """
        {"category":"Shape","bestMatch":"Unknown","recognizedText":"","alternatives":[],"confidence":0.2,"explanation":"Unclear."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Unknown, result.Category);
        Assert.Equal(ShapeType.Unknown, result.BestMatch);
    }

    [Fact]
    public void Parse_Letter_ReturnsRecognizedLetterAndAlternatives()
    {
        const string Json = """
        {
          "category": "Letter",
          "bestMatch": "Unknown",
          "recognizedText": "m",
          "alternatives": ["Letter N", "M"],
          "confidence": 0.74,
          "explanation": "Two peaks are visible."
        }
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Letter, result.Category);
        Assert.Equal(ShapeType.Unknown, result.BestMatch);
        Assert.Equal("M", result.RecognizedText);
        Assert.Single(result.Alternatives);
        Assert.Equal("N", result.Alternatives[0].RecognizedText);
    }

    [Fact]
    public void Parse_InvalidLetter_ReturnsUnknown()
    {
        const string Json = """
        {"category":"Letter","bestMatch":"Unknown","recognizedText":"MN","alternatives":[],"confidence":0.5,"explanation":"Unclear letter."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Unknown, result.Category);
        Assert.Null(result.RecognizedText);
    }

    [Fact]
    public void Parse_Digit_ReturnsRecognizedDigitAndAlternative()
    {
        const string Json = """
        {"category":"Digit","bestMatch":"Unknown","recognizedText":"Digit 8","alternatives":["3"],"confidence":0.76,"explanation":"Two loops are visible."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Digit, result.Category);
        Assert.Equal("8", result.RecognizedText);
        Assert.Equal("3", Assert.Single(result.Alternatives).RecognizedText);
    }

    [Fact]
    public void Parse_Text_ReturnsRecognizedWordAndAlternative()
    {
        const string Json = """
        {"category":"Text","bestMatch":"Unknown","recognizedText":"POLICE","alternatives":["POLICE" , "POLICE!"],"confidence":0.83,"explanation":"The handwritten word is readable."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Text, result.Category);
        Assert.Equal("POLICE", result.RecognizedText);
        Assert.Single(result.Alternatives);
        Assert.Equal("POLICE!", result.Alternatives[0].RecognizedText);
    }

    [Fact]
    public void Parse_UnknownCategory_ReturnsUnknown()
    {
        const string Json = """
        {"category":"Animal","bestMatch":"Unknown","recognizedText":"Cat","alternatives":[],"confidence":0.5,"explanation":"Unexpected category."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Unknown, result.Category);
        Assert.Equal(ShapeType.Unknown, result.BestMatch);
        Assert.Null(result.RecognizedText);
    }

    [Fact]
    public void Parse_InconsistentCategoryAndShape_ReturnsUnknown()
    {
        const string Json = """
        {"category":"Letter","bestMatch":"Circle","recognizedText":"M","alternatives":[],"confidence":0.5,"explanation":"Inconsistent result."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(DrawingCategory.Unknown, result.Category);
        Assert.Equal(ShapeType.Unknown, result.BestMatch);
    }

    [Fact]
    public void Parse_ClampsConfidenceAboveOne()
    {
        const string Json = """
        {"category":"Digit","bestMatch":"Unknown","recognizedText":"8","alternatives":[],"confidence":1.7,"explanation":""}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public void Parse_ClampsConfidenceBelowZero()
    {
        const string Json = """
        {"category":"Digit","bestMatch":"Unknown","recognizedText":"3","alternatives":[],"confidence":-0.4,"explanation":""}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public void Parse_RemovesDuplicateAndPrimaryAlternatives()
    {
        const string Json = """
        {"category":"Shape","bestMatch":"Circle","recognizedText":"","alternatives":["Circle","Ellipse","ellipse","Rectangle"],"confidence":0.8,"explanation":""}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(2, result.Alternatives.Count);
        Assert.Equal(ShapeType.Ellipse, result.Alternatives[0].Shape);
        Assert.Equal(ShapeType.Rectangle, result.Alternatives[1].Shape);
    }

    [Fact]
    public void Parse_DropsUnsupportedShapeAlternatives()
    {
        const string Json = """
        {"category":"Shape","bestMatch":"Circle","recognizedText":"","alternatives":["Pentagon","Ellipse","Hexagon"],"confidence":0.8,"explanation":"Almost round."}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Single(result.Alternatives);
        Assert.Equal(ShapeType.Ellipse, result.Alternatives[0].Shape);
    }

    [Fact]
    public void Parse_LimitsAlternativesToThree()
    {
        const string Json = """
        {"category":"Shape","bestMatch":"Circle","recognizedText":"","alternatives":["Square","Rectangle","Ellipse","Line"],"confidence":0.8,"explanation":""}
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Json);

        Assert.Equal(3, result.Alternatives.Count);
    }

    [Fact]
    public void Parse_JsonInsideMarkdownFence_StillParses()
    {
        const string Response = """
        ```json
        {"category":"Letter","bestMatch":"Unknown","recognizedText":"M","alternatives":["N"],"confidence":0.74,"explanation":"Two peaks are visible."}
        ```
        """;

        ShapeSuggestion result = ShapeSuggestionParser.Parse(Response);

        Assert.Equal(DrawingCategory.Letter, result.Category);
        Assert.Equal("M", result.RecognizedText);
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
        Assert.DoesNotContain("**", result);
        Assert.DoesNotContain("##", result);
        Assert.DoesNotContain("`", result);
    }
}
