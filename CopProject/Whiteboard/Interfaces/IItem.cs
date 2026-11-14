/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.Interfaces;

/// <summary>
/// Represents a whiteboard item
/// </summary>
public interface IItem
{
    /// <summary>
    /// Unique ID of the item
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Z-index of the item
    /// </summary>
    double Zindex { get; set; }

    /// <summary>
    /// User who created the item
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// Date and time of creation
    /// </summary>
    DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// Date and time of last modification
    /// </summary>
    DateTime LastModifiedDateTime { get; set; }

    /// <summary>
    /// X-coordinate of the leftmost edge of the item's bounding box
    /// </summary>
    double Left { get; }

    /// <summary>
    /// Y-coordinate of the topmost edge of the item's bounding box
    /// </summary>
    double Top { get; }

    /// <summary>
    /// Orientation (rotation) of the item in degrees
    /// </summary>
    double Orientation { get; set; }

    /// <summary>
    /// Minimal rectangle containing the item
    /// </summary>
    Rect2D BoundingBox { get; }

    /// <summary>
    /// Move the item by <paramref name="dX"/> rightwards and <paramref name="dY"/> downwards
    /// </summary>
    /// <param name="dX">Amount to move by, rightwards</param>
    /// <param name="dY">Amount to move by, downwards</param>
    void MoveBy(double dX, double dY);

    /// <summary>
    /// Check if a point intersects with the item
    /// </summary>
    /// <param name="point">Point to test</param>
    /// <returns>true if it intersects, false otherwise</returns>
    bool HitTest(Point2D point);
}

/// <summary>
/// Represents a point in the whiteboard
/// </summary>
/// <param name="X">X-coordinate of the point</param>
/// <param name="Y">Y-coordinate of the point</param>
[ExcludeFromCodeCoverage]
public record Point2D(
    double X,
    double Y
);

/// <summary>
/// Represents an always-upright rectangle on the whiteboard (not a UI element)
/// </summary>
/// <param name="Left">X-coordinate of the leftmost edge of the rectangle</param>
/// <param name="Top">Y-coordinate of the topmost edge of the rectangle</param>
/// <param name="Width">Width of the rectangle</param>
/// <param name="Height">Height of the rectangle</param>
[ExcludeFromCodeCoverage]
public record Rect2D(
    double Left,
    double Top,
    double Width,
    double Height
);
