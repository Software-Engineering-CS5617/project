using WhiteboardSuggestion.Core;
using Xunit;

namespace WhiteboardSuggestion.Tests;

public sealed class ShapeSuggestionFormatterTests
{
    [Fact]
    public void Format_ProducesReadablePlainTextForShape()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Shape,
            ShapeType.Circle,
            null,
            new[]
            {
                new SuggestionCandidate(DrawingCategory.Shape, ShapeType.Ellipse, null, false),
                new SuggestionCandidate(DrawingCategory.Shape, ShapeType.Rectangle, null, false)
            },
            0.876,
            "## It is nearly round with slight irregularities.");

        string output = ShapeSuggestionFormatter.Format(suggestion);

        Assert.Contains("Suggestion: Circle", output);
        Assert.Contains("Category: Shape", output);
        Assert.Contains("Other possibilities: Ellipse, Rectangle", output);
        Assert.Contains("Confidence: 88%", output);
        Assert.Contains("Reason: It is nearly round with slight irregularities.", output);
        Assert.DoesNotContain("**", output);
        Assert.DoesNotContain("##", output);
        Assert.DoesNotContain("```", output);
    }

    [Fact]
    public void Format_ProducesRecognizedTextForDigit()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Digit,
            ShapeType.Unknown,
            "8",
            Array.Empty<SuggestionCandidate>(),
            0.65,
            string.Empty);

        string output = ShapeSuggestionFormatter.Format(suggestion);

        Assert.Contains("Suggestion: Digit 8", output);
        Assert.Contains("Category: Digit", output);
        Assert.DoesNotContain("Other possibilities:", output);
        Assert.DoesNotContain("Reason:", output);
    }

    [Fact]
    public void Format_UnknownShowsUnknown()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Unknown,
            ShapeType.Unknown,
            null,
            Array.Empty<SuggestionCandidate>(),
            0.2,
            "The drawing is ambiguous.");

        string output = ShapeSuggestionFormatter.Format(suggestion);

        Assert.Contains("Suggestion: Unknown", output);
        Assert.Contains("Category: Unknown", output);
    }
}
