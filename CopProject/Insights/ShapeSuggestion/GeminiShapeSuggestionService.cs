using System.Text.Json;
using System.Text.Json.Nodes;
using Google.GenAI;
using Google.GenAI.Types;
using Trace = System.Diagnostics.Trace;

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Uses Gemini to recognize a shape from ordered stroke coordinates and estimate its geometry.
/// </summary>
public sealed class GeminiShapeSuggestionService : IShapeSuggestionService
{
    private const string ResponseSchemaJson = """
    {
      "type": "object",
      "properties": {
        "type": {
          "type": "string",
          "enum": ["Unknown", "Line", "Polygon", "Circle", "Ellipse"]
        },
        "confidence": { "type": "number", "minimum": 0, "maximum": 1 },
        "startX": { "type": "number" },
        "startY": { "type": "number" },
        "endX": { "type": "number" },
        "endY": { "type": "number" },
        "vertices": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "x": { "type": "number" },
              "y": { "type": "number" }
            },
            "required": ["x", "y"],
            "additionalProperties": false
          }
        },
        "centerX": { "type": "number" },
        "centerY": { "type": "number" },
        "radius": { "type": "number", "minimum": 0 },
        "radiusX": { "type": "number", "minimum": 0 },
        "radiusY": { "type": "number", "minimum": 0 }
      },
      "required": [
        "type", "confidence", "startX", "startY", "endX", "endY",
        "vertices", "centerX", "centerY", "radius", "radiusX", "radiusY"
      ],
      "additionalProperties": false
    }
    """;

    private const string Prompt = """
    You recognize one geometric shape from an ordered sequence of 2D stroke points.

    The input points are supplied in drawing order and use the Whiteboard canvas coordinate
    system. Infer the intended clean shape and return its geometric representation in that
    SAME coordinate system. Do not normalize coordinates or change the coordinate origin.

    Choose exactly one result type:
    - "Unknown": the trajectory is too ambiguous or does not represent a supported form.
    - "Line": a straight, open line segment. Return its Start and End points.
    - "Polygon": any closed straight-sided shape. Return simplified corner vertices in perimeter
      order, with at least three distinct vertices. Do not repeat the first vertex at the end.
    - "Circle": a circular shape. Return its centre and positive radius.
    - "Ellipse": an axis-aligned ellipse. Return its centre and positive horizontal and vertical radii.

    Preserve the input coordinate system and scale. Do not guess when the drawing is ambiguous.
    Confidence must be between 0 and 1. Return only the JSON object required by the response schema.
    Set unused numeric fields to 0 and vertices to an empty array unless the result is a Polygon.
    """;

    private readonly Func<string, string, GenerateContentConfig, Task<string?>> _generateContentAsync;
    private readonly string _modelName;

    public GeminiShapeSuggestionService(string apiKey, string? modelName = null)
    {
        Trace.TraceInformation("[Insights] Gemini shape suggestion service initialization started.");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Trace.TraceError("[Insights] Gemini shape suggestion service requires a non-empty API key.");
            throw new ArgumentException("A Gemini API key is required.", nameof(apiKey));
        }

        var client = new Client(apiKey: apiKey);
        _modelName = ResolveModelName(
            modelName,
            System.Environment.GetEnvironmentVariable("GEMINI_MODEL"));
        _generateContentAsync = CreateGenerator(client);

        Trace.TraceInformation(
            "[Insights] Gemini shape suggestion service initialized with model '{0}'.",
            _modelName);
    }

    // This factory keeps the service testable without making live Gemini API calls.
    internal static GeminiShapeSuggestionService CreateForTesting(
        Func<string, string, GenerateContentConfig, Task<string?>> generateContentAsync,
        string modelName)
    {
        ArgumentNullException.ThrowIfNull(generateContentAsync);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        return new GeminiShapeSuggestionService(generateContentAsync, modelName, forTesting: true);
    }

    private GeminiShapeSuggestionService(
        Func<string, string, GenerateContentConfig, Task<string?>> generateContentAsync,
        string modelName,
        bool forTesting)
    {
        _generateContentAsync = generateContentAsync;
        _modelName = modelName;
    }

    internal string ModelName => _modelName;

    public async Task<ShapeSuggestionResult> SuggestShapeAsync(StrokeRecognitionRequest request)
    {
        Trace.TraceInformation("[Insights] Stroke-based shape recognition started.");
        ValidateRequest(request);

        Trace.TraceInformation(
            "[Insights] Stroke request validated. Point count: {0}.",
            request.Points.Count);

        try
        {
            // Serialize tuple points as explicit x/y objects. System.Text.Json does not
            // serialize ValueTuple fields by default unless IncludeFields is enabled.
            string pointsJson = JsonSerializer.Serialize(
                request.Points.Select(point => new { x = point.X, y = point.Y }));

            string prompt = $"""
            {Prompt}

            Stroke points (ordered):
            {pointsJson}
            """;

            var config = new GenerateContentConfig
            {
                ResponseJsonSchema = JsonNode.Parse(ResponseSchemaJson),
                ResponseMimeType = "application/json",
                Temperature = 0.1
            };

            Trace.TraceInformation(
                "[Insights] Sending stroke coordinates to Gemini using model '{0}'.",
                _modelName);

            string? responseText = await _generateContentAsync(_modelName, prompt, config);

            Trace.TraceInformation("[Insights] Gemini shape recognition response received.");
            ShapeSuggestionResult result = ShapeSuggestionParser.Parse(responseText ?? string.Empty);
            Trace.TraceInformation(
                "[Insights] Stroke-based shape recognition completed with result '{0}'.",
                result.GetType().Name);

            return result;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Insights] Stroke-based shape recognition failed: {0}", ex);
            throw;
        }
    }

    internal static string ResolveModelName(string? modelName, string? environmentModel)
    {
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            return modelName;
        }

        if (!string.IsNullOrWhiteSpace(environmentModel))
        {
            return environmentModel;
        }

        return "gemini-3.5-flash-lite";
    }

    private static void ValidateRequest(StrokeRecognitionRequest request)
    {
        if (request is null)
        {
            Trace.TraceWarning("[Insights] Shape recognition rejected a null stroke request.");
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Points is null)
        {
            Trace.TraceWarning("[Insights] Shape recognition rejected a request with null stroke points.");
            throw new ArgumentException("Stroke points are required.", nameof(request));
        }

        if (request.Points.Count < 2)
        {
            Trace.TraceWarning(
                "[Insights] Shape recognition rejected a stroke containing fewer than two points.");
            throw new ArgumentException(
                "At least two stroke points are required.",
                nameof(request));
        }

        if (request.Points.Any(point =>
            !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
        {
            Trace.TraceWarning(
                "[Insights] Shape recognition rejected non-finite stroke coordinates.");
            throw new ArgumentException(
                "Stroke coordinates must contain finite X and Y values.",
                nameof(request));
        }
    }

    // This method is only the thin Google SDK transport adapter. Service behaviour is
    // covered with the injectable delegate constructor; live calls are integration tests.
    private static Func<string, string, GenerateContentConfig, Task<string?>> CreateGenerator(Client client)
    {
        return async (model, prompt, config) =>
        {
            GenerateContentResponse response = await client.Models.GenerateContentAsync(
                model: model,
                contents: prompt,
                config: config);

            return response.Text;
        };
    }
}
