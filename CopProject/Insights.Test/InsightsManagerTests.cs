using Insights.AI;
using Xunit;

namespace Insights.Test;

public class InsightsManagerTests
{
    [Fact]
    public async Task RequestTextAnalysisAsync_UsesActiveProvider()
    {
        var provider = new FakeInsightsProvider("TestModel");
        var manager = new InsightsManager(provider);

        string result = await manager.RequestTextAnalysisAsync("Test incident");

        Assert.Equal("Text result from TestModel", result);
        Assert.Equal("Test incident", provider.LastText);
    }

    [Fact]
    public async Task RequestImageAnalysisAsync_UsesActiveProvider()
    {
        var provider = new FakeInsightsProvider("TestModel");
        var manager = new InsightsManager(provider);

        byte[] image = { 1, 2, 3 };
        string prompt = "Analyse this image";

        string result = await manager.RequestImageAnalysisAsync(image, prompt);

        Assert.Equal("Image result from TestModel", result);
        Assert.Same(image, provider.LastImage);
        Assert.Equal(prompt, provider.LastPrompt);
    }

    [Fact]
    public async Task SwitchModel_UsesNewProvider()
    {
        var firstProvider = new FakeInsightsProvider("FirstModel");
        var secondProvider = new FakeInsightsProvider("SecondModel");

        var manager = new InsightsManager(firstProvider);

        manager.SwitchModel(secondProvider);

        string result = await manager.RequestTextAnalysisAsync("Test incident");

        Assert.Equal("Text result from SecondModel", result);
        Assert.Equal(0, firstProvider.TextCallCount);
        Assert.Equal(1, secondProvider.TextCallCount);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_WithoutContext_UsesActiveProvider()
    {
        var provider = new FakeInsightsProvider("TestModel");
        var manager = new InsightsManager(provider);

        string question = "What are traffic signals?";

        string result = await manager.GenerateChatResponseAsync(question);

        Assert.Equal("Chat response from TestModel", result);
        Assert.Equal(question, provider.LastUserMessage);
        Assert.Null(provider.LastChatContext);
        Assert.Equal(1, provider.ChatCallCount);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_WithContext_PassesContextToProvider()
    {
        var provider = new FakeInsightsProvider("TestModel");
        var manager = new InsightsManager(provider);

        string question = "Why did the accident happen?";
        string context = "Alice: It was raining. Bob: The road was slippery.";

        string result = await manager.GenerateChatResponseAsync(question, context);

        Assert.Equal("Chat response from TestModel", result);
        Assert.Equal(question, provider.LastUserMessage);
        Assert.Equal(context, provider.LastChatContext);
        Assert.Equal(1, provider.ChatCallCount);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_AfterSwitch_UsesNewProvider()
    {
        var firstProvider = new FakeInsightsProvider("FirstModel");
        var secondProvider = new FakeInsightsProvider("SecondModel");

        var manager = new InsightsManager(firstProvider);

        manager.SwitchModel(secondProvider);

        string question = "Why did the accident happen?";
        string context = "It was raining and the road was slippery.";

        string result = await manager.GenerateChatResponseAsync(
            question,
            context);

        Assert.Equal("Chat response from SecondModel", result);
        Assert.Equal(0, firstProvider.ChatCallCount);
        Assert.Equal(1, secondProvider.ChatCallCount);
        Assert.Equal(context, secondProvider.LastChatContext);
    }

    [Fact]
    public async Task RequestTextAnalysisAsync_WhenProviderFails_RethrowsException()
    {
        var manager = new InsightsManager(new ThrowingInsightsProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.RequestTextAnalysisAsync("incident"));
    }

    [Fact]
    public async Task RequestImageAnalysisAsync_WhenProviderFails_RethrowsException()
    {
        var manager = new InsightsManager(new ThrowingInsightsProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.RequestImageAnalysisAsync(new byte[] { 1 }, "prompt"));
    }

    [Fact]
    public async Task GenerateChatResponseAsync_WhenProviderFails_RethrowsException()
    {
        var manager = new InsightsManager(new ThrowingInsightsProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.GenerateChatResponseAsync("question"));
    }

    private sealed class ThrowingInsightsProvider : IInsightsProvider
    {
        public string ModelName => "ThrowingProvider";

        public Task<string> AnalyzeTrafficIncidentAsync(string textDescription) =>
            Task.FromException<string>(new InvalidOperationException("provider failed"));

        public Task<string> AnalyzeImageAsync(byte[] imageData, string prompt) =>
            Task.FromException<string>(new InvalidOperationException("provider failed"));

        public Task<string> GenerateChatResponseAsync(string userMessage, string? chatContext = null) =>
            Task.FromException<string>(new InvalidOperationException("provider failed"));
    }

    private class FakeInsightsProvider : IInsightsProvider
    {
        public string ModelName { get; }

        public string? LastText { get; private set; }
        public byte[]? LastImage { get; private set; }
        public string? LastPrompt { get; private set; }
        public string? LastUserMessage { get; private set; }
        public string? LastChatContext { get; private set; }
        public int ChatCallCount { get; private set; }

        public int TextCallCount { get; private set; }

        public FakeInsightsProvider(string modelName)
        {
            ModelName = modelName;
        }

        public Task<string> AnalyzeTrafficIncidentAsync(string textDescription)
        {
            LastText = textDescription;
            TextCallCount++;

            return Task.FromResult($"Text result from {ModelName}");
        }

        public Task<string> AnalyzeImageAsync(byte[] imageData, string prompt)
        {
            LastImage = imageData;
            LastPrompt = prompt;

            return Task.FromResult($"Image result from {ModelName}");
        }

        public Task<string> GenerateChatResponseAsync(
            string userMessage,
            string? chatContext = null)
        {
            LastUserMessage = userMessage;
            LastChatContext = chatContext;
            ChatCallCount++;

            return Task.FromResult($"Chat response from {ModelName}");
        }
    }
}
