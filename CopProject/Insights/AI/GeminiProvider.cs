using System.Collections.Generic;
using Trace = System.Diagnostics.Trace;
using Google.GenAI;
using Google.GenAI.Types;

namespace Insights.AI;

internal delegate Task<string?> GeminiContentGenerator(
    string model,
    string? textContents,
    List<Content>? contentItems,
    GenerateContentConfig? config);

public class GeminiProvider : IInsightsProvider
{
    private readonly GeminiContentGenerator _generateContentAsync;

    public string ModelName => "gemini-3.5-flash";

    // Used by the factory and unit tests to supply the Gemini request implementation.
    internal GeminiProvider(GeminiContentGenerator generateContentAsync)
    {
        _generateContentAsync = generateContentAsync
            ?? throw new ArgumentNullException(nameof(generateContentAsync));
    }

    public static async Task<GeminiProvider> CreateAsync(
        ICloudCredentialService cloudConfig)
    {
        Trace.TraceInformation("[Insights] GeminiProvider initialization started.");

        try
        {
            ArgumentNullException.ThrowIfNull(cloudConfig);

            Trace.TraceInformation("[Insights] Retrieving Gemini API credentials.");
            string apiKey = await cloudConfig.GetSecretAsync("GEMINI_API_KEY");

            var client = new Client(apiKey: apiKey);
            var provider = new GeminiProvider(CreateGenerator(client));

            Trace.TraceInformation(
                "[Insights] GeminiProvider initialized successfully using model '{0}'.",
                provider.ModelName);

            return provider;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] GeminiProvider initialization failed: {0}", ex);
            throw;
        }
    }

    public async Task<string> AnalyzeTrafficIncidentAsync(string textDescription)
    {
        Trace.TraceInformation("[Insights] Traffic incident text analysis started.");

        try
        {
            Trace.TraceInformation(
                "[Insights] Sending traffic incident analysis request to Gemini using model '{0}'.",
                ModelName);

            string? responseText = await _generateContentAsync(
                ModelName,
                textDescription,
                contentItems: null,
                config: null);

            Trace.TraceInformation("[Insights] Gemini traffic incident analysis response received.");
            if (string.IsNullOrWhiteSpace(responseText))
            {
                Trace.TraceWarning("[Insights] Gemini returned an empty traffic incident analysis response.");
            }

            return responseText ?? string.Empty;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] Gemini traffic incident analysis failed: {0}", ex);
            return $"[Gemini Error]: {ex.Message}";
        }
    }

    public async Task<string> AnalyzeImageAsync(byte[] imageData, string prompt)
    {
        Trace.TraceInformation(
            "[Insights] Image analysis started. Image size: {0} bytes.",
            imageData?.Length ?? 0);

        try
        {
            Trace.TraceInformation(
                "[Insights] Sending image analysis request to Gemini using model '{0}'.",
                ModelName);

            var contents = new List<Content>
            {
                new()
                {
                    Parts = new List<Part>
                    {
                        new() { Text = prompt },
                        new()
                        {
                            InlineData = new Blob
                            {
                                MimeType = "image/jpeg",
                                Data = imageData
                            }
                        }
                    }
                }
            };

            string? responseText = await _generateContentAsync(
                ModelName,
                textContents: null,
                contentItems: contents,
                config: null);

            Trace.TraceInformation("[Insights] Gemini image analysis response received.");
            if (string.IsNullOrWhiteSpace(responseText))
            {
                Trace.TraceWarning("[Insights] Gemini returned an empty image analysis response.");
            }

            return responseText ?? string.Empty;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] Gemini image analysis failed: {0}", ex);
            return $"[Gemini Image Error]: {ex.Message}";
        }
    }

    public async Task<string> GenerateChatResponseAsync(
        string userMessage,
        string? chatContext = null)
    {
        Trace.TraceInformation("[Insights] Chat response generation started.");

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            Trace.TraceWarning(
                "[Insights] Chat response request rejected because the user message is empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

        try
        {
            Trace.TraceInformation(
                "[Insights] Preparing Gemini chat prompt. Context supplied: {0}.",
                !string.IsNullOrWhiteSpace(chatContext));

            string prompt = string.IsNullOrWhiteSpace(chatContext)
                ? $"Latest user question:\n{userMessage}"
                : $"Chat context:\n{chatContext}\n\nLatest user question:\n{userMessage}";

            GenerateContentConfig config = new()
            {
                SystemInstruction = new Content
                {
                    Parts = new List<Part>
                    {
                        new()
                        {
                            Text = """
                            Keep responses brief and direct.
                            Limit each response to 5-6 short lines and approximately 60 words.
                            Answer only what the user asked.
                            Avoid unnecessary introductions, headings, examples and explanations.
                            Use plain text without Markdown formatting.
                            For context-based questions, use only relevant conversation details.
                            For general questions, answer using general knowledge.
                            Provide more details only when explicitly requested.
                            """
                        }
                    }
                }
            };

            Trace.TraceInformation(
                "[Insights] Sending chat response request to Gemini using model '{0}'.",
                ModelName);

            string? responseText = await _generateContentAsync(
                ModelName,
                prompt,
                contentItems: null,
                config: config);

            Trace.TraceInformation("[Insights] Gemini chat response received.");
            if (string.IsNullOrWhiteSpace(responseText))
            {
                Trace.TraceWarning("[Insights] Gemini returned an empty chat response.");
            }

            return responseText ?? string.Empty;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] Gemini chat response generation failed: {0}", ex);
            return $"[Gemini Chat Error]: {ex.Message}";
        }
    }

    // The adapter is a thin wrapper around the external Google SDK and is covered by live
    // integration testing rather than deterministic unit tests.
    private static GeminiContentGenerator CreateGenerator(Client client)
    {
        return (model, textContents, contentItems, config) =>
            GenerateWithClientAsync(client, model, textContents, contentItems, config);
    }

    private static async Task<string?> GenerateWithClientAsync(
        Client client,
        string model,
        string? textContents,
        List<Content>? contentItems,
        GenerateContentConfig? config)
    {
        GenerateContentResponse response = textContents is not null
            ? await client.Models.GenerateContentAsync(model: model, contents: textContents, config: config)
            : await client.Models.GenerateContentAsync(model: model, contents: contentItems!, config: config);

        return response.Text;
    }
}
