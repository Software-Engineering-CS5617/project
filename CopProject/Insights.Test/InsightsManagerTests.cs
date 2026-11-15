// Authors:
// Adwaith Nath R - Roll Number: 112614010
// Hariram S - Roll Number: 112614008

using Insights.AI;
using Google.GenAI.Types;
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

    [Fact]
    public async Task DiagnoseLogAsync_UsesActiveProviderAndReturnsFormattedDiagnosis()
    {
        var provider = new FakeInsightsProvider("TestModel")
        {
            StructuredResponse = """
            {
                "summary": "Database connection failed.",
                "failureDetected": true,
                "failureLocation": "DatabaseConnector.Connect",
                "likelyCause": "Invalid connection string.",
                "recommendations": ["Verify the connection string."]
            }
            """
        };

        var manager = new InsightsManager(provider);
        string log = "[ERROR] Database connection failed.";

        string result = await manager.DiagnoseLogAsync(log);

        Assert.Contains("Summary: Database connection failed.", result);
        Assert.Contains("Failure detected: Yes", result);
        Assert.Contains("Failure location: DatabaseConnector.Connect", result);
        Assert.Contains("Likely cause: Invalid connection string.", result);
        Assert.Contains("Verify the connection string.", result);

        Assert.Equal(1, provider.StructuredCallCount);
        Assert.NotNull(provider.LastStructuredPrompt);
        Assert.Contains(log, provider.LastStructuredPrompt);
    }

    [Fact]
    public async Task DiagnoseLogAsync_EmptyLog_ThrowsArgumentException()
    {
        var provider = new FakeInsightsProvider("TestModel");
        var manager = new InsightsManager(provider);

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.DiagnoseLogAsync(" "));

        Assert.Equal(0, provider.StructuredCallCount);
    }

    [Fact]
    public async Task DiagnoseLogAsync_UnsupportedProvider_ThrowsInvalidOperationException()
    {
        var manager = new InsightsManager(new ThrowingInsightsProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.DiagnoseLogAsync("Application log"));
    }

    [Fact]
    public async Task DiagnoseLogAsync_WhenProviderFails_RethrowsException()
    {
        var provider = new FakeInsightsProvider("TestModel")
        {
            StructuredException = new InvalidOperationException("Gemini request failed.")
        };

        var manager = new InsightsManager(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.DiagnoseLogAsync("Application log"));

        Assert.Equal(1, provider.StructuredCallCount);
    }

    [Fact]
    public async Task DiagnoseLogAsync_WhenNoFailureAndNoRecommendations_ReturnsCorrectMessage()
    {
        var provider = new FakeInsightsProvider("TestModel")
        {
            StructuredResponse = """
            {
                "summary": "No errors found in the application log.",
                "failureDetected": false,
                "failureLocation": null,
                "likelyCause": null,
                "recommendations": []
            }
            """
        };

        var manager = new InsightsManager(provider);

        string result = await manager.DiagnoseLogAsync(
            "Application started successfully.");

        Assert.Contains("Summary: No errors found", result);
        Assert.Contains("Failure detected: No", result);
        Assert.Contains("No recommendations provided.", result);

        Assert.DoesNotContain("Failure location:", result);
        Assert.DoesNotContain("Likely cause:", result);
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

    private class FakeInsightsProvider : IInsightsProvider, ILogAnalysisProvider
    {
        public string ModelName { get; }

        public string? LastText { get; private set; }
        public byte[]? LastImage { get; private set; }
        public string? LastPrompt { get; private set; }
        public string? LastUserMessage { get; private set; }
        public string? LastChatContext { get; private set; }
        public int ChatCallCount { get; private set; }
        public int StructuredCallCount { get; private set; }
        public string? LastStructuredModel { get; private set; }
        public string? LastStructuredPrompt { get; private set; }
        public Exception? StructuredException { get; set; }

        public string StructuredResponse { get; set; } = """
        {
            "summary": "No failure detected.",
            "failureDetected": false,
            "failureLocation": null,
            "likelyCause": null,
            "recommendations": []
        }
        """;


        public int TextCallCount { get; private set; }

        public Task<string?> GenerateStructuredContentAsync(
            string model,
            string prompt,
            GenerateContentConfig config)
        {
            StructuredCallCount++;
            LastStructuredModel = model;
            LastStructuredPrompt = prompt;

            if (StructuredException is not null)
            {
                return Task.FromException<string?>(StructuredException);
            }

            return Task.FromResult<string?>(StructuredResponse);
        }
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
