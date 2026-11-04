using WhiteboardSuggestion.Core;
using Xunit;

namespace WhiteboardSuggestion.Tests;

public sealed class ShapeSuggestionFormatterTests
{
    [Fact]
    public void Format_ProducesReadablePlainText()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Shape,
            "**Circle**",
            new[] { "Oval", "Circle" },
            0.876,
            "## It is nearly round with slight irregularities.");

        string output = ShapeSuggestionFormatter.Format(suggestion);

        Assert.Contains("Suggestion: Circle", output);
        Assert.Contains("Category: Shape", output);
        Assert.Contains("Other possibilities: Oval", output);
        Assert.Contains("Confidence: 88%", output);
        Assert.Contains("Reason: It is nearly round with slight irregularities.", output);
        Assert.DoesNotContain("**", output);
        Assert.DoesNotContain("##", output);
        Assert.DoesNotContain("```", output);
    }

    [Fact]
    public void Format_NoAlternatives_OmitsAlternativesLine()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Digit,
            "Digit 3",
            Array.Empty<string>(),
            0.65,
            string.Empty);

        string output = ShapeSuggestionFormatter.Format(suggestion);

        Assert.Contains("Suggestion: Digit 3", output);
        Assert.DoesNotContain("Other possibilities:", output);
        Assert.DoesNotContain("Reason:", output);
    }
}
