// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Network.Dtos;
using Windows.UI;

namespace Whiteboard.Network.Mappers;

public class ItemMapper
{
    /// <summary>
    /// Map an <see cref="IItem"/> to corresponding <see cref="ItemDto"/>
    /// </summary>
    /// <param name="item">Item to map</param>
    /// <returns>Corresponding ItemDto</returns>
    /// <exception cref="UnreachableException"></exception>
    public static ItemDto ToDto(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        ItemMetadata metadata = new(
            item.Id,
            item.Zindex,
            item.CreatedBy,
            item.CreatedDateTime,
            item.LastModifiedDateTime,
            item.Orientation
        );

        ItemPayload payload = item switch {
            IFreehand freehand => new ItemPayload.Freehand(
                freehand.Points,
                ColorToHex(freehand.StrokeColor),
                freehand.StrokeWidth
            ),

            ILine line => new ItemPayload.Line(
                line.Begin,
                line.End
            ),

            ITextbox textbox => new ItemPayload.Textbox(
                textbox.Center,
                textbox.Content,
                textbox.FontFamily,
                textbox.FontSize,
                ColorToHex(textbox.FontColor)
            ),

            ICircle circle => new ItemPayload.Circle(
                ToShapeStyle(circle),
                circle.Center,
                circle.Radius
            ),

            IEllipse ellipse => new ItemPayload.Ellipse(
                ToShapeStyle(ellipse),
                ellipse.Center,
                ellipse.Width,
                ellipse.Height
            ),

            IEqTriangle eqTriangle => new ItemPayload.EqTriangle(
                ToShapeStyle(eqTriangle),
                eqTriangle.Center,
                eqTriangle.SideLength
            ),

            IPolygon polygon => new ItemPayload.Polygon(
                ToShapeStyle(polygon),
                polygon.Vertices
            ),

            IRectangle rectangle => new ItemPayload.Rectangle(
                ToShapeStyle(rectangle),
                rectangle.Center,
                rectangle.Width,
                rectangle.Height
            ),

            ISquare square => new ItemPayload.Square(
                ToShapeStyle(square),
                square.Center,
                square.SideLegth
            ),

            _ => throw new UnreachableException()
        };

        ItemDto itemDto = new(metadata, payload);
        return itemDto;
    }

    public static IItem FromDto(ItemDto itemDto)
    {
        ArgumentNullException.ThrowIfNull(itemDto);

        IItem item = itemDto.Payload switch {
            ItemPayload.Freehand freehand => new FreehandItem {
                Points = freehand.Points,
                StrokeColor = HexToColor(freehand.StrokeColor),
                StrokeWidth = freehand.StrokWidth
            },

            ItemPayload.Line line => new Line {
                Begin = line.Begin,
                End = line.End
            },

            ItemPayload.Textbox textbox => new Textbox {
                Center = textbox.Center,
                Content = textbox.Content,
                FontFamily = textbox.FontFamily,
                FontSize = textbox.FontSize,
                FontColor = HexToColor(textbox.FontColor)
            },

            ItemPayload.Circle circle => new Circle {
                Center = circle.Center,
                Radius = circle.Radius,
                FillColor = HexToColor(circle.Style.FillColor),
                BorderColor = HexToColor(circle.Style.BorderColor),
                BorderWidth = circle.Style.BorderWidth
            },

            ItemPayload.Ellipse ellipse => new Ellipse {
                Center = ellipse.Center,
                Width = ellipse.Width,
                Height = ellipse.Height,
                FillColor = HexToColor(ellipse.Style.FillColor),
                BorderColor = HexToColor(ellipse.Style.BorderColor),
                BorderWidth = ellipse.Style.BorderWidth
            },

            ItemPayload.EqTriangle eqTriangle => new EqTriangle {
                Center = eqTriangle.Center,
                Left = eqTriangle.Center.X - (eqTriangle.SideLength / 2),
                Top = eqTriangle.Center.Y - (eqTriangle.SideLength * Math.Sqrt(3) / 4),
                SideLength = eqTriangle.SideLength,
                FillColor = HexToColor(eqTriangle.Style.FillColor),
                BorderColor = HexToColor(eqTriangle.Style.BorderColor),
                BorderWidth = eqTriangle.Style.BorderWidth
            },

            ItemPayload.Polygon polygon => new Polygon {
                Vertices = polygon.Vertices,
                FillColor = HexToColor(polygon.Style.FillColor),
                BorderColor = HexToColor(polygon.Style.BorderColor),
                BorderWidth = polygon.Style.BorderWidth
            },

            ItemPayload.Rectangle rectangle => new Rectangle {
                Center = rectangle.Center,
                Width = rectangle.Width,
                Height = rectangle.Height,
                Left = rectangle.Center.X - (rectangle.Width / 2),
                Top = rectangle.Center.Y - (rectangle.Height / 2),
                FillColor = HexToColor(rectangle.Style.FillColor),
                BorderColor = HexToColor(rectangle.Style.BorderColor),
                BorderWidth = rectangle.Style.BorderWidth
            },

            ItemPayload.Square square => new Square {
                Center = square.Center,
                SideLegth = square.SideLength,
                Left = square.Center.X - (square.SideLength / 2),
                Top = square.Center.Y - (square.SideLength / 2),
                FillColor = HexToColor(square.Style.FillColor),
                BorderColor = HexToColor(square.Style.BorderColor),
                BorderWidth = square.Style.BorderWidth
            },

            _ => throw new NotImplementedException()
        };

        item.Id = itemDto.Metadata.Id;
        item.Zindex = itemDto.Metadata.Zindex;
        item.CreatedBy = itemDto.Metadata.CreatedBy;
        item.CreatedDateTime = itemDto.Metadata.CreatedDateTime;
        item.LastModifiedDateTime = itemDto.Metadata.LastModifiedDateTime;
        item.Orientation = itemDto.Metadata.Orientation;

        return item;
    }

    /// <summary>
    /// Return <see cref="ShapeStyle"/> for a given <see cref="IShape"/>
    /// </summary>
    /// <param name="shape">A shape</param>
    /// <returns>ShapeStyle</returns>
    private static ShapeStyle ToShapeStyle(IShape shape)
    {
        return new ShapeStyle(ColorToHex(shape.FillColor), ColorToHex(shape.BorderColor), shape.BorderWidth);
    }

    /// <summary>
    /// Convert a <see cref="Color"/> to its hexadecimal value
    /// </summary>
    /// <param name="color">Color to convert</param>
    /// <returns>String hex value of the color</returns>
    public static string ColorToHex(Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static readonly Color s_default_color = Color.FromArgb(255, 0, 0, 0);

    /// <summary>
    /// Convert a hexadecimal color value to <see cref="Color"/>
    /// </summary>
    /// <param name="color">String hex value of the color<</param>
    /// <returns>Color</returns>
    public static Color HexToColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return s_default_color;
        }

        hex = hex.TrimStart('#');
        if (hex.Length == 8 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint val))
        {
            return Color.FromArgb(
                (byte)((val >> 24) & 0xFF),
                (byte)((val >> 16) & 0xFF),
                (byte)((val >> 8) & 0xFF),
                (byte)(val & 0xFF)
            );
        }

        return s_default_color;
    }
}

