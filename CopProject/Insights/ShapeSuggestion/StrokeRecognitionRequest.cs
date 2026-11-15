// Author: Hariram S
// Roll Number: 112614008

namespace WhiteboardSuggestion.Core;

/// <summary>
/// Ordered 2D points captured during one whiteboard drawing gesture.
/// Coordinates use the Whiteboard canvas coordinate system.
/// </summary>
public sealed record StrokeRecognitionRequest(
    IReadOnlyList<(double X, double Y)> Points);
