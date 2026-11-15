/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Network.Dtos;
using Whiteboard.Network.Serializations;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Serialization.RoundTripPreservesData;

public class ItemDto_RPD
{
    /// <summary>
    /// Test that string serialization and deserialization preserves the data
    /// </summary>
    /// <param name="type">One of <see cref="XUnitShapeType"/></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Theory]
    [InlineData(XUnitShapeType.Freehand)]
    [InlineData(XUnitShapeType.Line)]
    [InlineData(XUnitShapeType.Textbox)]
    [InlineData(XUnitShapeType.Circle)]
    [InlineData(XUnitShapeType.Ellipse)]
    [InlineData(XUnitShapeType.EqTriangle)]
    [InlineData(XUnitShapeType.Polygon)]
    [InlineData(XUnitShapeType.Rectangle)]
    [InlineData(XUnitShapeType.Square)]
    public void TestString(XUnitShapeType type)
    {
        // Repeat test 50 times
        for (int i = 0; i < 50; i++)
        {
            // Arrange
            ItemMetadata meta = RandomItemMetadataGenerator.Generate();
            ItemPayload payload = type switch {
                XUnitShapeType.Freehand => RandomItemPayloadGenerator.GenerateFreehand(),
                XUnitShapeType.Line => RandomItemPayloadGenerator.GenerateLine(),
                XUnitShapeType.Textbox => RandomItemPayloadGenerator.GenerateTextbox(),
                XUnitShapeType.Circle => RandomItemPayloadGenerator.GenerateCircle(),
                XUnitShapeType.Ellipse => RandomItemPayloadGenerator.GenerateEllipse(),
                XUnitShapeType.EqTriangle => RandomItemPayloadGenerator.GenerateEqTriangle(),
                XUnitShapeType.Polygon => RandomItemPayloadGenerator.GeneratePolygon(),
                XUnitShapeType.Rectangle => RandomItemPayloadGenerator.GenerateRectangle(),
                XUnitShapeType.Square => RandomItemPayloadGenerator.GenerateSquare(),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            ItemDto original = new ItemDto(meta, payload);

            // Act
            string serialized = ItemDtoStringSerializer.Serialize(original);
            ItemDto deserialized = ItemDtoStringSerializer.Deserialize(serialized);

            // Assert
            Assert.Equivalent(original, deserialized);
        }
    }

    /// <summary>
    /// Test that byte serialization and deserialization preserves the data
    /// </summary>
    /// <param name="type">One of <see cref="XUnitShapeType"/></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Theory]
    [InlineData(XUnitShapeType.Freehand)]
    [InlineData(XUnitShapeType.Line)]
    [InlineData(XUnitShapeType.Textbox)]
    [InlineData(XUnitShapeType.Circle)]
    [InlineData(XUnitShapeType.Ellipse)]
    [InlineData(XUnitShapeType.EqTriangle)]
    [InlineData(XUnitShapeType.Polygon)]
    [InlineData(XUnitShapeType.Rectangle)]
    [InlineData(XUnitShapeType.Square)]
    public void TestByte(XUnitShapeType type)
    {
        // Repeat test 50 times
        for (int i = 0; i < 50; i++)
        {
            // Arrange
            ItemMetadata meta = RandomItemMetadataGenerator.Generate();
            ItemPayload payload = type switch {
                XUnitShapeType.Freehand => RandomItemPayloadGenerator.GenerateFreehand(),
                XUnitShapeType.Line => RandomItemPayloadGenerator.GenerateLine(),
                XUnitShapeType.Textbox => RandomItemPayloadGenerator.GenerateTextbox(),
                XUnitShapeType.Circle => RandomItemPayloadGenerator.GenerateCircle(),
                XUnitShapeType.Ellipse => RandomItemPayloadGenerator.GenerateEllipse(),
                XUnitShapeType.EqTriangle => RandomItemPayloadGenerator.GenerateEqTriangle(),
                XUnitShapeType.Polygon => RandomItemPayloadGenerator.GeneratePolygon(),
                XUnitShapeType.Rectangle => RandomItemPayloadGenerator.GenerateRectangle(),
                XUnitShapeType.Square => RandomItemPayloadGenerator.GenerateSquare(),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            ItemDto original = new ItemDto(meta, payload);

            // Act
            byte[] serialized = new ItemDtoByteSerializer().Serialize(original);
            ItemDto deserialized = new ItemDtoByteSerializer().Deserialize(serialized);

            // Assert
            Assert.Equivalent(original, deserialized);
        }
    }

}
