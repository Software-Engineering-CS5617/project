/// Owners  : Raagam Hitesh Parmar 
/// Roll No : 112301024
/// LICENSE : MIT

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Whiteboard.Interfaces;

namespace Whiteboard.Network.Dtos;

/// <summary>
/// Describes common properties of every whiteboard item
/// </summary>
/// <param name="Id">Unique ID of the item</param>
/// <param name="Zindex">Z-indez of the item</param>
/// <param name="CreatedBy">User who created the item</param>
/// <param name="CreatedDateTime">Date and time of creation</param>
/// <param name="LastModifiedDateTime">Date and time of last modification</param>
/// <param name="Orientation">Orientation (rotation) of the item in degrees</param>
[ExcludeFromCodeCoverage]
public record ItemMetadata(
    Guid Id,
    double Zindex,
    string CreatedBy,
    DateTime CreatedDateTime,
    DateTime LastModifiedDateTime,
    double Orientation
);

/// <summary>
/// Describes common properties of every whiteboard shape item
/// </summary>
/// <param name="FillColor">Fill color</param>
/// <param name="BorderColor">Border color</param>
/// <param name="BorderWidth">Border Width</param>
[ExcludeFromCodeCoverage]
public record ShapeStyle(
    string FillColor,
    string BorderColor,
    double BorderWidth
);

/// <summary>
/// Describes the properties of an item, apart from the ones carried by <see cref="ItemMetadata"/>
/// </summary>
// $type determines which derived type to instantiate during (de-)serialization.
// The name of said type is determined by the rest of the decorators.
// For example, { ..., "$type" : "freehand", ... } or { ..., "$type" : "square", ... }, etc.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]

// Freehand item
[JsonDerivedType(typeof(Freehand), "freehand")]

// Line item
[JsonDerivedType(typeof(Line), "line")]

// Textbox item
[JsonDerivedType(typeof(Textbox), "textbox")]

// Shape item
[JsonDerivedType(typeof(Circle), "circle")]
[JsonDerivedType(typeof(Ellipse), "ellipse")]
[JsonDerivedType(typeof(EqTriangle), "triangle")]
[JsonDerivedType(typeof(Polygon), "polygon")]
[JsonDerivedType(typeof(Rectangle), "rectangle")]
[JsonDerivedType(typeof(Square), "square")]
[ExcludeFromCodeCoverage]
public abstract record ItemPayload
{
    [ExcludeFromCodeCoverage]
    public sealed record Freehand(List<Point2D> Points, string StrokeColor, double StrokWidth) : ItemPayload;
    [ExcludeFromCodeCoverage]
    public sealed record Line(Point2D Begin, Point2D End, string Color, double Width) : ItemPayload;
    [ExcludeFromCodeCoverage]
    public sealed record Textbox(Point2D Center, string Content, string FontFamily, int FontSize, string FontColor) : ItemPayload;
    [ExcludeFromCodeCoverage]
    public sealed record Circle(ShapeStyle Style, Point2D Center, double Radius) : ItemPayload;
    [ExcludeFromCodeCoverage]
    public sealed record Ellipse(ShapeStyle Style, Point2D Center, double Width, double Height) : ItemPayload;
    [ExcludeFromCodeCoverage]
    public sealed record EqTriangle(ShapeStyle Style, Point2D Center, double SideLength) : ItemPayload;
    [ExcludeFromCodeCoverage]
    public sealed record Polygon(ShapeStyle Style, List<Point2D> Vertices) : ItemPayload;
    [ExcludeFromCodeCoverage]
    public sealed record Rectangle(ShapeStyle Style, Point2D Center, double Width, double Height) : ItemPayload;
    [ExcludeFromCodeCoverage]
    public sealed record Square(ShapeStyle Style, Point2D Center, double SideLength) : ItemPayload;
}

/// <summary>
/// Describes a serializable representation of a whiteboard item
/// </summary>
/// <param name="Metadata">Item Metadata</param>
/// <param name="Payload">Item Payload</param>
[ExcludeFromCodeCoverage]
public record ItemDto(
    ItemMetadata Metadata,
    ItemPayload Payload
);
