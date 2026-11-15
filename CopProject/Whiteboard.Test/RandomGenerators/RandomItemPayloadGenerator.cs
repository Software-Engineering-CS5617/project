/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Interfaces;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="ItemPayload"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomItemPayloadGenerator : BaseGenerator
{
    /// <summary>
    /// Generate a random <see cref="ItemPayload.Freehand"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Freehand"/></returns>
    public static ItemPayload.Freehand GenerateFreehand()
    {
        // 1. Generate a random list of points
        List<Point2D> points = RandomPointListGenerator.Generate(2, 500);

        // 2. Generate a random stroke color
        string strokeColor = RandomColorGenerator.Generate().ToString();

        // 3. Generate a random stroke width
        double strokeWidth = Rng.NextDouble() * 100;

        // 4. Generate a freehand payload
        ItemPayload.Freehand freehand = new(points, strokeColor, strokeWidth);
        return freehand;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Line"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Line"/></returns>
    public static ItemPayload.Line GenerateLine()
    {
        // 1. Generate a two random endpoints
        Point2D begin = RandomPointGenerator.Generate();
        Point2D end = RandomPointGenerator.Generate();

        // 2. Generate random color and thickness
        string color = RandomColorGenerator.Generate().ToString();
        double width = Rng.NextDouble() * 50;

        // 3. Generate a line payload
        ItemPayload.Line line = new(begin, end, color, width);
        return line;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Textbox"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Textbox"/></returns>
    public static ItemPayload.Textbox GenerateTextbox()
    {
        // 1. Generate a two random center
        Point2D center = RandomPointGenerator.Generate();

        // 2. Generate a random string
        string content = LoremIpsumGenerator.Generate();

        // 3. Generate a random font family
        string fontFamily = RandomFontFamilyGenerator.Generate();

        // 4. Generate a random font size
        int fontSize = Rng.Next(1, 10);

        // 5. Generate a random font color
        string fontColor = RandomColorGenerator.Generate().ToString();

        // 6. Generate a textbox payload
        ItemPayload.Textbox textbox = new(center, content, fontFamily, fontSize, fontColor);
        return textbox;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Circle"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Circle"/></returns>
    public static ItemPayload.Circle GenerateCircle()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        Point2D center = RandomPointGenerator.Generate();

        // 3. Generate a random radius
        double radius = Rng.NextDouble() * 100;

        // 4. Generate a circle itempayload
        ItemPayload.Circle circle = new(style, center, radius);
        return circle;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Ellipse"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Ellipse"/></returns>
    public static ItemPayload.Ellipse GenerateEllipse()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        Point2D center = RandomPointGenerator.Generate();

        // 3. Generate a random width and height
        double width = Rng.NextDouble() * 100;
        double height = Rng.NextDouble() * 100;

        // 4. Generate a ellipse itempayload
        ItemPayload.Ellipse ellipse = new(style, center, width, height);
        return ellipse;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.EqTriangle"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.EqTriangle"/></returns>
    public static ItemPayload.EqTriangle GenerateEqTriangle()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        Point2D center = RandomPointGenerator.Generate();

        // 3. Generate a random side length
        double sideLength = Rng.NextDouble() * 100;

        // 4. Generate a eqilateral triangle itempayload
        ItemPayload.EqTriangle eqTriangle = new(style, center, sideLength);
        return eqTriangle;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Polygon"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Polygon"/></returns>
    public static ItemPayload.Polygon GeneratePolygon()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random lit of vertices
        List<Point2D> vertices = RandomPointListGenerator.Generate();

        // 3. Generate a polygon itempayload
        ItemPayload.Polygon polygon = new(style, vertices);
        return polygon;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Rectangle"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Rectangle"/></returns>
    public static ItemPayload.Rectangle GenerateRectangle()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        Point2D center = RandomPointGenerator.Generate();

        // 3. Generate a random width and height
        double width = Rng.NextDouble() * 100;
        double height = Rng.NextDouble() * 100;

        // 4. Generate a rectangle itempayload
        ItemPayload.Rectangle rectangle = new(style, center, width, height);
        return rectangle;
    }

    /// <summary>
    /// Generate a random <see cref="ItemPayload.Square"/>
    /// </summary>
    /// <returns>A random <see cref="ItemPayload.Square"/></returns>
    public static ItemPayload.Square GenerateSquare()
    {
        // 1. Generate a random style
        ShapeStyle style = RandomShapeStyleGenerator.Generate();

        // 2. Generate a random center
        Point2D center = RandomPointGenerator.Generate();

        // 3. Generate a random side length
        double sideLength = Rng.NextDouble() * 100;

        // 4. Generate a square itempayload
        ItemPayload.Square square = new(style, center, sideLength);
        return square;
    }
}
