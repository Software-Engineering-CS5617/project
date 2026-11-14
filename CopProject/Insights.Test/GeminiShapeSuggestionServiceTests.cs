using Google.GenAI.Types;
using WhiteboardSuggestion.Core;
using Xunit;

namespace Insights.Test;

public sealed class GeminiShapeSuggestionServiceTests
{
    private const string ValidLineResponse = """
    {"type":"Line","confidence":0.9,"startX":10,"startY":20,"endX":30,"endY":40,
     "vertices":[],"centerX":0,"centerY":0,"radius":0,"radiusX":0,"radiusY":0}
    """;

    [Fact]
    public void Constructor_RejectsMissingApiKey()
    {
        Assert.Throws<ArgumentException>(() => new GeminiShapeSuggestionService(null!));
        Assert.Throws<ArgumentException>(() => new GeminiShapeSuggestionService(""));
        Assert.Throws<ArgumentException>(() => new GeminiShapeSuggestionService("   "));
    }

    [Fact]
    public void ResolveModelName_UsesExplicitEnvironmentOrDefaultModel()
    {
        Assert.Equal("explicit", GeminiShapeSuggestionService.ResolveModelName("explicit", "environment"));
        Assert.Equal("environment", GeminiShapeSuggestionService.ResolveModelName(" ", "environment"));
        Assert.Equal("gemini-3.5-flash-lite", GeminiShapeSuggestionService.ResolveModelName(null, null));
    }

    [Fact]
    public void CreateForTesting_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => GeminiShapeSuggestionService.CreateForTesting(null!, "model"));
        Assert.Throws<ArgumentException>(() => GeminiShapeSuggestionService.CreateForTesting((_, _, _) => Task.FromResult<string?>(ValidLineResponse), " "));
    }

    [Fact]
    public async Task SuggestShapeAsync_ValidStroke_SendsCoordinatesAndParsesResponse()
    {
        string? model = null;
        string? prompt = null;
        GenerateContentConfig? config = null;
        var service = GeminiShapeSuggestionService.CreateForTesting((m, p, c) =>
        {
            model = m;
            prompt = p;
            config = c;
            return Task.FromResult<string?>(ValidLineResponse);
        }, "test-model");

        var request = new StrokeRecognitionRequest(new[] { (10.0, 20.0), (30.0, 40.0) });
        var result = Assert.IsType<ShapeSuggestionResult.Line>(await service.SuggestShapeAsync(request));

        Assert.Equal((10.0, 20.0), result.Start);
        Assert.Equal("test-model", model);
        Assert.Contains("{\"x\":10,\"y\":20}", prompt);
        Assert.Contains("{\"x\":30,\"y\":40}", prompt);
        Assert.Equal("application/json", config!.ResponseMimeType);
        Assert.NotNull(config.ResponseJsonSchema);
    }

    [Fact]
    public async Task SuggestShapeAsync_NullRequest_ThrowsArgumentNullException()
    {
        var service = CreateService(ValidLineResponse);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SuggestShapeAsync(null!));
    }

    [Fact]
    public async Task SuggestShapeAsync_NullPoints_ThrowsArgumentException()
    {
        var service = CreateService(ValidLineResponse);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SuggestShapeAsync(new StrokeRecognitionRequest(null!)));
    }

    [Fact]
    public async Task SuggestShapeAsync_RequiresAtLeastTwoPoints()
    {
        var service = CreateService(ValidLineResponse);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SuggestShapeAsync(new StrokeRecognitionRequest(Array.Empty<(double, double)>() )));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SuggestShapeAsync(new StrokeRecognitionRequest(new[] { (1.0, 2.0) })));
    }

    [Fact]
    public async Task SuggestShapeAsync_RejectsNonFiniteCoordinates()
    {
        var service = CreateService(ValidLineResponse);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SuggestShapeAsync(
            new StrokeRecognitionRequest(new[] { (double.NaN, 2.0), (3.0, 4.0) })));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SuggestShapeAsync(
            new StrokeRecognitionRequest(new[] { (1.0, 2.0), (3.0, double.PositiveInfinity) })));
    }

    [Fact]
    public async Task SuggestShapeAsync_EmptyResponse_ThrowsFormatException()
    {
        var service = CreateService(null);
        await Assert.ThrowsAsync<FormatException>(() => service.SuggestShapeAsync(CreateRequest()));
    }

    [Fact]
    public async Task SuggestShapeAsync_GeminiFailure_PropagatesException()
    {
        var service = GeminiShapeSuggestionService.CreateForTesting(
            (_, _, _) => Task.FromException<string?>(new InvalidOperationException("Gemini unavailable")),
            "test-model");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuggestShapeAsync(CreateRequest()));
    }

    private static GeminiShapeSuggestionService CreateService(string? response) =>
        GeminiShapeSuggestionService.CreateForTesting((_, _, _) => Task.FromResult(response), "test-model");

    private static StrokeRecognitionRequest CreateRequest() =>
        new(new[] { (1.0, 2.0), (3.0, 4.0) });
}
