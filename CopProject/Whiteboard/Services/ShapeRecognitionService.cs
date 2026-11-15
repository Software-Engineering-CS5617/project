/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using WhiteboardSuggestion.Core;
using Windows.UI;

namespace Whiteboard.Services;

/// <inheritdoc/>
public class ShapeRecognitionService : IShapeRecognitionService
{
    private IShapeSuggestionService? _suggestionService;

    /// <summary>
    /// Default confidence threshold for accepting a recognized shape suggestion
    /// </summary>
    public const double DefaultConfidenceThreshold = 0.65;

    public bool IsEnabled { get; set; } = false;

    public double ConfidenceThreshold { get; set; } = DefaultConfidenceThreshold;

    public ShapeRecognitionService(IShapeSuggestionService? suggestionService = null)
    {
        if (suggestionService is not null)
        {
            _suggestionService = suggestionService;
        }
        else
        {
            _ = GetSuggestionService();
        }
    }

    /// <summary>
    /// Create a new shape suggestion service using <see cref="GeminiShapeSuggestionService"/>
    /// </summary>
    private IShapeSuggestionService? GetSuggestionService()
    {
        if (_suggestionService is not null)
        {
            return _suggestionService;
        }

        string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                _suggestionService = new GeminiShapeSuggestionService(apiKey);
                Trace.TraceInformation("[Whiteboard] Initialized GeminiShapeSuggestionService successfully with GEMINI_API_KEY.");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[Whiteboard] Could not initialize GeminiShapeSuggestionService: {ex.Message}");
            }
        }

        return _suggestionService;
    }

    public async Task<IItem?> TryRecognizeShapeAsync(IFreehand freehand, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            Trace.TraceInformation("[Whiteboard] Shape recognition skipped: Smart Shape is not enabled (toggle is OFF).");
            return null;
        }

        if (freehand is null || freehand.Points is null || freehand.Points.Count < 2)
        {
            Trace.TraceInformation("[Whiteboard] Shape recognition skipped: stroke has fewer than 2 points.");
            return null;
        }

        IShapeSuggestionService? suggestionService = GetSuggestionService();
        if (suggestionService is null)
        {
            Trace.TraceWarning("[Whiteboard] Shape recognition skipped: GEMINI_API_KEY environment variable is not set. Stroke kept as raw sketch.");
            return null;
        }

        try
        {
            var points = freehand.Points.Select(p => (p.X, p.Y)).ToList();
            StrokeRecognitionRequest request = new(points);

            Trace.TraceInformation($"[Whiteboard] Requesting shape suggestion for stroke with {points.Count} points.");
            ShapeSuggestionResult result = await suggestionService.SuggestShapeAsync(request).ConfigureAwait(false);

            if (result is null || result.Confidence < ConfidenceThreshold)
            {
                Trace.TraceInformation($"[Whiteboard] Shape suggestion rejected: Result={result?.GetType().Name}, Confidence={result?.Confidence}, Threshold={ConfidenceThreshold}");
                return null;
            }

            IItem? recognizedItem = ConvertResultToItem(result, freehand);
            if (recognizedItem is not null)
            {
                Trace.TraceInformation($"[Whiteboard] Shape suggestion accepted: Converted to {recognizedItem.GetType().Name} with confidence {result.Confidence:F2}.");
            }

            return recognizedItem;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Whiteboard] Shape recognition call failed: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Converts an Insights <see cref="ShapeSuggestionResult"/> to a concrete Whiteboard <see cref="IItem"/>.
    /// </summary>
    public static IItem? ConvertResultToItem(ShapeSuggestionResult result, IFreehand freehand)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(freehand);

        Color transparentFill = Color.FromArgb(0, 0, 0, 0);

        return result switch {
            ShapeSuggestionResult.Line lineResult => new Line {
                Begin = new Point2D(lineResult.Start.X, lineResult.Start.Y),
                End = new Point2D(lineResult.End.X, lineResult.End.Y),
                Color = freehand.StrokeColor,
                Width = freehand.StrokeWidth,
                CreatedBy = freehand.CreatedBy,
                Zindex = freehand.Zindex
            },

            ShapeSuggestionResult.Circle circleResult => new Circle {
                Center = new Point2D(circleResult.Center.X, circleResult.Center.Y),
                Radius = Math.Max(1.0, circleResult.Radius),
                BorderColor = freehand.StrokeColor,
                BorderWidth = freehand.StrokeWidth,
                FillColor = transparentFill,
                CreatedBy = freehand.CreatedBy,
                Zindex = freehand.Zindex
            },

            ShapeSuggestionResult.Ellipse ellipseResult => new Ellipse {
                Center = new Point2D(ellipseResult.Center.X, ellipseResult.Center.Y),
                Width = Math.Max(1.0, ellipseResult.RadiusX * 2.0),
                Height = Math.Max(1.0, ellipseResult.RadiusY * 2.0),
                BorderColor = freehand.StrokeColor,
                BorderWidth = freehand.StrokeWidth,
                FillColor = transparentFill,
                CreatedBy = freehand.CreatedBy,
                Zindex = freehand.Zindex
            },

            ShapeSuggestionResult.Polygon polygonResult when polygonResult.Vertices is not null && polygonResult.Vertices.Count >= 3 => new Polygon {
                Vertices = [.. polygonResult.Vertices.Select(v => new Point2D(v.X, v.Y))],
                BorderColor = freehand.StrokeColor,
                BorderWidth = freehand.StrokeWidth,
                FillColor = transparentFill,
                CreatedBy = freehand.CreatedBy,
                Zindex = freehand.Zindex
            },

            _ => null
        };
    }
}
