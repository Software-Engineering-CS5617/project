/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// An enum representing differen test cases for xUnit
/// </summary>
public enum XUnitShapeType
{
    Freehand,
    Line,
    Textbox,
    Circle,
    Ellipse,
    EqTriangle,
    Polygon,
    Rectangle,
    Square
}

/// <summary>
/// Generates a random <see cref="ItemDto"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomItemDtoGenerator : BaseGenerator
{
    private static readonly XUnitShapeType[] s_availableShapes = Enum.GetValues<XUnitShapeType>();

    public static ItemDto Generate(XUnitShapeType? type = null)
    {
        // 1. Pick the shape provided by the user or a random shape
        XUnitShapeType selectedType = type ?? s_availableShapes[Rng.Next(s_availableShapes.Length)];

        ItemPayload payload = selectedType switch {
            XUnitShapeType.Freehand => RandomItemPayloadGenerator.GenerateFreehand(),
            XUnitShapeType.Line => RandomItemPayloadGenerator.GenerateLine(),
            XUnitShapeType.Textbox => RandomItemPayloadGenerator.GenerateTextbox(),
            XUnitShapeType.Circle => RandomItemPayloadGenerator.GenerateCircle(),
            XUnitShapeType.Ellipse => RandomItemPayloadGenerator.GenerateEllipse(),
            XUnitShapeType.EqTriangle => RandomItemPayloadGenerator.GenerateEqTriangle(),
            XUnitShapeType.Polygon => RandomItemPayloadGenerator.GeneratePolygon(),
            XUnitShapeType.Rectangle => RandomItemPayloadGenerator.GenerateRectangle(),
            XUnitShapeType.Square => RandomItemPayloadGenerator.GenerateSquare(),
            _ => throw new System.Diagnostics.UnreachableException()
        };

        // 2. Generate some random item metadata
        ItemMetadata meta = RandomItemMetadataGenerator.Generate();

        // 3. Generate random Item dto
        ItemDto itemDto = new(meta, payload);
        return itemDto;
    }
}
