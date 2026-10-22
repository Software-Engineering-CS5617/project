using System;

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
    /// User who last modified the item
    /// </summary>
    string LastModifiedBy { get; set; }

    /// <summary>
    /// Date and time of last modification
    /// </summary>
    DateTime LastModifiedDateTime { get; set; }

    /// <summary>
    /// X-coordinate of the leftmost edge of the item's bounding box
    /// </summary>
    double Left { get; set; }

    /// <summary>
    /// Y-coordinate of the topmost edge of the item's bounding box
    /// </summary>
    double Top { get; set; }

    /// <summary>
    /// Orientation (rotation) of the item in degrees
    /// </summary>
    double Orientation { get; set; }

    /// <summary>
    /// Minimal rectangle containing the item
    /// </summary>
    Rect2D BoundingBox { get; set; }

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
public struct Point2D
{
    /// <summary>
    /// X-coordinate of the point
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y-coordinate of the point
    /// </summary>
    public double Y { get; set; }
}

/// <summary>
/// Represents an always-upright rectangle on the whiteboard (not a UI element)
/// </summary>
public struct Rect2D
{
    /// <summary>
    /// X-coordinate of the leftmost edge of the rectangle
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// Y-coordinate of the topmost edge of the rectangle
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// Width of the rectangle
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Height of the rectangle
    /// </summary>
    public double Height { get; set; }
}
