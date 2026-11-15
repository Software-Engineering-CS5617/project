/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Mappers;

public class ItemMapperRoundTripTests
{
    /// <summary>
    /// Tests that round trip from Dto to Model to Dto preserves all data
    /// </summary>
    /// <param name="type">Type of Dto to generate</param>
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
    public void RoundTrip_DtoToModelToDto_PreservesAllData(XUnitShapeType type)
    {
        for (int i = 0; i < 50; i++)
        {
            // Arrange
            ItemDto originalDto = RandomItemDtoGenerator.Generate(type);

            // Act
            IItem model = ItemMapper.FromDto(originalDto);
            ItemDto mappedBackDto = ItemMapper.ToDto(model);

            // Assert
            Assert.Equivalent(originalDto, mappedBackDto);
        }
    }
}
