using Google.GenAI.Types;
using Insights.AI;
using Xunit;

namespace Insights.Test;

public sealed class GeminiProviderTests
{
    [Fact]
    public void Constructor_NullGenerator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new GeminiProvider(null!));
    }

    [Fact]
    public void ModelName_ReturnsConfiguredModelName()
    {
        var provider = CreateProvider((_, _, _, _) => Task.FromResult<string?>("ok"));

        Assert.Equal("gemini-3.5-flash", provider.ModelName);
    }

    [Fact]
    public async Task CreateAsync_UsesCredentialServiceAndReturnsProvider()
    {
        var credentials = new FakeCredentialService("test-api-key");

        GeminiProvider provider = await GeminiProvider.CreateAsync(credentials);

        Assert.Equal("GEMINI_API_KEY", credentials.LastSecretName);
        Assert.Equal("gemini-3.5-flash", provider.ModelName);
    }

    [Fact]
    public async Task CreateAsync_NullCredentialService_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => GeminiProvider.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_WhenCredentialLookupFails_RethrowsException()
    {
        var credentials = new FakeCredentialService(exception: new InvalidOperationException("missing key"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => GeminiProvider.CreateAsync(credentials));
    }

    [Fact]
    public async Task AnalyzeTrafficIncidentAsync_ReturnsGeminiResponse()
    {
        string? capturedModel = null;
        object? capturedContents = null;
        var provider = CreateProvider((model, textContents, _, _) =>
        {
            capturedModel = model;
            capturedContents = textContents;
            return Task.FromResult<string?>("Traffic analysis result");
        });

        string result = await provider.AnalyzeTrafficIncidentAsync("Vehicle collision on the road");

        Assert.Equal("Traffic analysis result", result);
        Assert.Equal("gemini-3.5-flash", capturedModel);
        Assert.Equal("Vehicle collision on the road", capturedContents);
    }

    [Fact]
    public async Task AnalyzeTrafficIncidentAsync_EmptyResponse_ReturnsEmptyString()
    {
        var provider = CreateProvider((_, _, _, _) => Task.FromResult<string?>(null));

        string result = await provider.AnalyzeTrafficIncidentAsync("Incident description");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task AnalyzeTrafficIncidentAsync_WhenGeminiFails_ReturnsErrorMessage()
    {
        var provider = CreateProvider((_, _, _, _) =>
            Task.FromException<string?>(new InvalidOperationException("service unavailable")));

        string result = await provider.AnalyzeTrafficIncidentAsync("Incident description");

        Assert.Equal("[Gemini Error]: service unavailable", result);
    }

    [Fact]
    public async Task AnalyzeImageAsync_SendsImageAndPromptAndReturnsResponse()
    {
        object? capturedContents = null;
        var provider = CreateProvider((_, _, contentItems, _) =>
        {
            capturedContents = contentItems;
            return Task.FromResult<string?>("Image analysis result");
        });
        byte[] image = { 1, 2, 3, 4 };

        string result = await provider.AnalyzeImageAsync(image, "Describe this image");

        Assert.Equal("Image analysis result", result);
        var contentList = Assert.IsType<List<Content>>(capturedContents);
        Assert.Single(contentList);

        var parts = contentList[0].Parts
            ?? throw new InvalidOperationException("Expected image content parts.");

        Assert.Equal("Describe this image", parts[0].Text);
        Assert.Equal("image/jpeg", parts[1].InlineData!.MimeType);
        Assert.Same(image, parts[1].InlineData!.Data);
    }

    [Fact]
    public async Task AnalyzeImageAsync_EmptyResponse_ReturnsEmptyString()
    {
        var provider = CreateProvider((_, _, _, _) => Task.FromResult<string?>(string.Empty));

        string result = await provider.AnalyzeImageAsync(new byte[] { 1 }, "Describe");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenGeminiFails_ReturnsErrorMessage()
    {
        var provider = CreateProvider((_, _, _, _) =>
            Task.FromException<string?>(new InvalidOperationException("image request failed")));

        string result = await provider.AnalyzeImageAsync(new byte[] { 1 }, "Describe");

        Assert.Equal("[Gemini Image Error]: image request failed", result);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_WithoutContext_SendsQuestionOnly()
    {
        object? capturedContents = null;
        GenerateContentConfig? capturedConfig = null;
        var provider = CreateProvider((_, textContents, _, config) =>
        {
            capturedContents = textContents;
            capturedConfig = config;
            return Task.FromResult<string?>("Chat result");
        });

        string result = await provider.GenerateChatResponseAsync("What happened?");

        Assert.Equal("Chat result", result);
        Assert.Equal("Latest user question:\nWhat happened?", capturedContents);
        Assert.NotNull(capturedConfig?.SystemInstruction);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_WithContext_IncludesContextAndQuestion()
    {
        object? capturedContents = null;
        var provider = CreateProvider((_, textContents, _, _) =>
        {
            capturedContents = textContents;
            return Task.FromResult<string?>("Contextual chat result");
        });

        string result = await provider.GenerateChatResponseAsync("Why?", "The road was wet.");

        Assert.Equal("Contextual chat result", result);
        Assert.Equal("Chat context:\nThe road was wet.\n\nLatest user question:\nWhy?", capturedContents);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_EmptyResponse_ReturnsEmptyString()
    {
        var provider = CreateProvider((_, _, _, _) => Task.FromResult<string?>(null));

        string result = await provider.GenerateChatResponseAsync("Hello");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_WhenGeminiFails_ReturnsErrorMessage()
    {
        var provider = CreateProvider((_, _, _, _) =>
            Task.FromException<string?>(new InvalidOperationException("chat request failed")));

        string result = await provider.GenerateChatResponseAsync("Hello");

        Assert.Equal("[Gemini Chat Error]: chat request failed", result);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_NullUserMessage_ThrowsArgumentNullException()
    {
        var provider = CreateProvider((_, _, _, _) => Task.FromResult<string?>("unused"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.GenerateChatResponseAsync(null!));
    }

    [Fact]
    public async Task GenerateChatResponseAsync_EmptyOrWhitespaceUserMessage_ThrowsArgumentException()
    {
        var provider = CreateProvider((_, _, _, _) => Task.FromResult<string?>("unused"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.GenerateChatResponseAsync(""));

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.GenerateChatResponseAsync("   "));
    }

    private static GeminiProvider CreateProvider(GeminiContentGenerator generator)
    {
        return new GeminiProvider(generator);
    }

    private sealed class FakeCredentialService : ICloudCredentialService
    {
        private readonly string? _secret;
        private readonly Exception? _exception;

        public string? LastSecretName { get; private set; }

        public FakeCredentialService(string secret)
        {
            _secret = secret;
        }

        public FakeCredentialService(Exception exception)
        {
            _exception = exception;
        }

        public Task<string> GetSecretAsync(string secretName)
        {
            LastSecretName = secretName;

            if (_exception is not null)
            {
                return Task.FromException<string>(_exception);
            }

            return Task.FromResult(_secret!);
        }
    }
}
