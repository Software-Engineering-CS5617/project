using WhiteboardSuggestion.Core;
using Xunit;

namespace WhiteboardSuggestion.Tests;

public sealed class ShapeSuggestionCandidateFactoryTests
{
    [Fact]
    public void Create_IncludesRecommendationAndAlternatives()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Shape,
            "Circle",
            new[] { "Oval", "Ellipse" },
            0.87,
            "Almost round.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Equal(3, candidates.Count);
        Assert.Equal("Circle", candidates[0].Label);
        Assert.True(candidates[0].IsRecommended);
        Assert.False(candidates[1].IsRecommended);
        Assert.Equal(DrawingCategory.Shape, candidates[1].Category);
        Assert.Equal("Oval", candidates[1].CanonicalValue);
    }

    [Fact]
    public void Create_NormalizesLetterForDrawing()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Letter,
            "Letter m",
            new[] { "Letter N" },
            0.8,
            "Two possible letters.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Equal("Letter M", candidates[0].Label);
        Assert.Equal("M", candidates[0].CanonicalValue);
        Assert.Equal(DrawingCategory.Letter, candidates[0].Category);
        Assert.Equal("N", candidates[1].CanonicalValue);
    }

    [Fact]
    public void Create_NormalizesDigitForDrawing()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Digit,
            "Digit 8",
            new[] { "Digit 3" },
            0.76,
            "Loops are visible.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Equal("8", candidates[0].CanonicalValue);
        Assert.Equal("3", candidates[1].CanonicalValue);
        Assert.Equal("Digit 3", candidates[1].Label);
    }

    [Fact]
    public void Create_RemovesDuplicateCandidatesCaseInsensitively()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Shape,
            "Circle",
            new[] { "circle", "Oval", "oval" },
            0.7,
            "Almost round.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Equal(2, candidates.Count);
        Assert.Equal(new[] { "Circle", "Oval" }, candidates.Select(candidate => candidate.Label));
    }

    [Fact]
    public void Create_DoesNotMakeUnclearASelectableCandidate()
    {
        var suggestion = new ShapeSuggestion(
            DrawingCategory.Unknown,
            "Unclear",
            Array.Empty<string>(),
            0.1,
            "The drawing is ambiguous.");

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);

        Assert.Empty(candidates);
    }
}
