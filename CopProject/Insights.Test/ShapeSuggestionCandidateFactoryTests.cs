using WhiteboardSuggestion.Core;
using Xunit;

namespace WhiteboardSuggestion.Tests;

public sealed class ShapeSuggestionCandidateFactoryTests
{
    [Fact]
    public void Create_IncludesTypedShapeRecommendationAndAlternatives()
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
            0.87,
            "Almost round.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Equal(3, candidates.Count);
        Assert.Equal(ShapeType.Circle, candidates[0].Shape);
        Assert.Equal("Circle", candidates[0].Label);
        Assert.True(candidates[0].IsRecommended);
        Assert.False(candidates[1].IsRecommended);
        Assert.Equal(ShapeType.Ellipse, candidates[1].Shape);
        Assert.Equal("Ellipse", candidates[1].Label);
    }

    [Fact]
    public void Create_FormatsEquilateralTriangleLabel()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Shape,
            ShapeType.EquilateralTriangle,
            null,
            Array.Empty<SuggestionCandidate>(),
            0.9,
            "Three similar sides.");

        SuggestionCandidate candidate = Assert.Single(ShapeSuggestionCandidateFactory.Create(suggestion));

        Assert.Equal("Equilateral Triangle", candidate.Label);
        Assert.Equal(ShapeType.EquilateralTriangle, candidate.Shape);
    }

    [Fact]
    public void Create_NormalizesLetterCandidate()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Letter,
            ShapeType.Unknown,
            "M",
            new[] { new SuggestionCandidate(DrawingCategory.Letter, ShapeType.Unknown, "N", false) },
            0.8,
            "Two possible letters.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Equal("Letter M", candidates[0].Label);
        Assert.Equal("M", candidates[0].RecognizedText);
        Assert.Equal("Letter N", candidates[1].Label);
    }

    [Fact]
    public void Create_FormatsDigitCandidate()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Digit,
            ShapeType.Unknown,
            "8",
            new[] { new SuggestionCandidate(DrawingCategory.Digit, ShapeType.Unknown, "3", false) },
            0.76,
            "Loops are visible.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Equal("Digit 8", candidates[0].Label);
        Assert.Equal("8", candidates[0].RecognizedText);
        Assert.Equal("Digit 3", candidates[1].Label);
    }

    [Fact]
    public void Create_FormatsTextCandidate()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Text,
            ShapeType.Unknown,
            "POLICE",
            Array.Empty<SuggestionCandidate>(),
            0.85,
            "The word is readable.");

        SuggestionCandidate candidate = Assert.Single(ShapeSuggestionCandidateFactory.Create(suggestion));

        Assert.Equal("POLICE", candidate.Label);
        Assert.Equal("POLICE", candidate.RecognizedText);
    }

    [Fact]
    public void Create_DoesNotReturnUnsupportedShape()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Shape,
            (ShapeType)999,
            null,
            new[] { new SuggestionCandidate(DrawingCategory.Shape, ShapeType.Circle, null, false) },
            0.8,
            "Invalid shape value.");

        Assert.Empty(ShapeSuggestionCandidateFactory.Create(suggestion));
    }

    [Fact]
    public void Create_DropsWrongCategoryAndDuplicateAlternatives()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Shape,
            ShapeType.Circle,
            null,
            new[]
            {
                new SuggestionCandidate(DrawingCategory.Shape, ShapeType.Circle, null, false),
                new SuggestionCandidate(DrawingCategory.Letter, ShapeType.Unknown, "M", false),
                new SuggestionCandidate(DrawingCategory.Shape, ShapeType.Ellipse, null, false)
            },
            0.8,
            "Almost round.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Equal(2, candidates.Count);
        Assert.Equal(ShapeType.Ellipse, candidates[1].Shape);
    }

    [Fact]
    public void Create_UnknownDoesNotProduceSelectableCandidate()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Unknown,
            ShapeType.Unknown,
            null,
            Array.Empty<SuggestionCandidate>(),
            0.1,
            "The drawing is ambiguous.");

        Assert.Empty(ShapeSuggestionCandidateFactory.Create(suggestion));
    }
}
