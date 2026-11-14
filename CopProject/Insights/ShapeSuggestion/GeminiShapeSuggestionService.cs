using System.Text.Json;
using System.Text.Json.Nodes;
using Google.GenAI;
using Google.GenAI.Types;

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Uses Gemini to recognize a shape from ordered stroke coordinates and estimate its geometry.
/// </summary>
public sealed class GeminiShapeSuggestionService : IShapeSuggestionService
{
    // A flat schema keeps the model response simple. Fields that do not apply to the
    // selected result type must be returned as zero; vertices must be empty unless type is Polygon.
    private const string ResponseSchemaJson = """
    {
      "type": "object",
      "properties": {
        "type": {
          "type": "string",
          "enum": ["Unknown", "Line", "Polygon", "Circle", "Ellipse"]
        },
        "confidence": {
          "type": "number",
          "minimum": 0,
          "maximum": 1
        },
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
        "type", "confidence",
        "startX", "startY", "endX", "endY",
        "vertices", "centerX", "centerY",
        "radius", "radiusX", "radiusY"
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

    - "Unknown": the trajectory is too ambiguous or does not represent one of the supported forms.
    - "Line": a straight, open line segment. Return its Start and End points.
    - "Polygon": any closed straight-sided shape, including triangles, squares, rectangles,
      diamonds and polygons with any number of sides. Return the simplified corner vertices
      in perimeter order. Do not repeat the first vertex at the end.
    - "Circle": a circular shape. Return its centre and positive radius.
    - "Ellipse": an oval or elliptical shape. Return its centre, positive horizontal radiusX,
      and positive vertical radiusY. The result is axis-aligned because the output contract
      does not contain a rotation angle.

    Geometry requirements:
    - Estimate clean geometry that closely follows the user's stroke.
    - Use coordinates in the same scale and coordinate space as the input points.
    - For Line, return two distinct endpoints.
    - For Polygon, return at least three distinct vertices, ordered around the boundary.
    - For Circle and Ellipse, return the centre and positive radii.
    - Do not classify a closed polygon as a Circle or Ellipse.
    - If the drawing cannot be recognized reliably, return Unknown instead of guessing.
    - Confidence must be a number from 0 to 1.

    The JSON response schema contains all geometry fields. Set fields that do not apply to the
    chosen type to 0, and set vertices to an empty array unless the type is Polygon.
    Return only the JSON object. Do not include Markdown or explanatory text outside JSON.
    """;

    private readonly Client _client;
    private readonly string _modelName;

    public GeminiShapeSuggestionService(string apiKey, string? modelName = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("A Gemini API key is required.", nameof(apiKey));
        }

        _client = new Client(apiKey: apiKey);
        string? environmentModel = System.Environment.GetEnvironmentVariable("GEMINI_MODEL");
        _modelName = !string.IsNullOrWhiteSpace(modelName)
            ? modelName
            : !string.IsNullOrWhiteSpace(environmentModel)
                ? environmentModel
                : "gemini-3.5-flash-lite";
    }

    public async Task<ShapeSuggestionResult> SuggestShapeAsync(
        StrokeRecognitionRequest request)
    {
        ValidateRequest(request);

        // Serialize tuple points as explicit x/y objects. System.Text.Json does not
        // serialize ValueTuple fields by default unless IncludeFields is enabled.
        string pointsJson = JsonSerializer.Serialize(
            request.Points.Select(point => new { x = point.X, y = point.Y }));

        string prompt = $"""
        {Prompt}

        Stroke points (ordered):
        {pointsJson}
        """;

        GenerateContentResponse response = await _client.Models.GenerateContentAsync(
            model: _modelName,
            contents: prompt,
            config: new GenerateContentConfig
            {
                ResponseJsonSchema = JsonNode.Parse(ResponseSchemaJson),
                ResponseMimeType = "application/json",
                Temperature = 0.1
            });

        return ShapeSuggestionParser.Parse(response.Text ?? string.Empty);
    }

    private static void ValidateRequest(StrokeRecognitionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Points is null)
        {
            throw new ArgumentException("Stroke points are required.", nameof(request));
        }

        if (request.Points.Count < 2)
        {
            throw new ArgumentException(
                "At least two stroke points are required.",
                nameof(request));
        }

        if (request.Points.Any(point =>
            !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
        {
            throw new ArgumentException(
                "Stroke coordinates must contain finite X and Y values.",
                nameof(request));
        }
    }
}
