namespace WhiteboardSuggestion.Core;

/// <summary>
/// Geometric recognition result returned to the Whiteboard module.
/// Confidence is the model-reported estimate, not a calibrated probability.
/// </summary>
public abstract record ShapeSuggestionResult(double Confidence)
{
    /// <summary>The stroke could not be recognized as a supported geometric form.</summary>
    public sealed record Unknown(double Confidence)
        : ShapeSuggestionResult(Confidence);

    /// <summary>A straight open line represented by its two endpoints.</summary>
    public sealed record Line(
        (double X, double Y) Start,
        (double X, double Y) End,
        double Confidence)
        : ShapeSuggestionResult(Confidence);

    /// <summary>A closed polygon with boundary vertices in perimeter order.</summary>
    public sealed record Polygon(
        IReadOnlyList<(double X, double Y)> Vertices,
        double Confidence)
        : ShapeSuggestionResult(Confidence);

    /// <summary>A circle represented by its centre and radius.</summary>
    public sealed record Circle(
        (double X, double Y) Center,
        double Radius,
        double Confidence)
        : ShapeSuggestionResult(Confidence);

    /// <summary>
    /// An axis-aligned ellipse represented by its centre and horizontal/vertical radii.
    /// The current contract does not include a rotation angle.
    /// </summary>
    public sealed record Ellipse(
        (double X, double Y) Center,
        double RadiusX,
        double RadiusY,
        double Confidence)
        : ShapeSuggestionResult(Confidence);
}
