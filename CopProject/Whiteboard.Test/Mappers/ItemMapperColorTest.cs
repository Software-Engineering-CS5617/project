/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Network.Mappers;
using Windows.UI;

namespace Whiteboard.Test.Mappers;

public class ItemMapperColorTests
{
    /// <summary>
    /// Test if a valid color returns a valid formatted hex
    /// </summary>
    [Fact]
    public void ColorToHex_ValidColor_ReturnsFormattedHex()
    {
        Color red = Color.FromArgb(255, 255, 0, 0);
        string hex = ItemMapper.ColorToHex(red);

        Assert.Equal("#FFFF0000", hex);
    }

    /// <summary>
    /// Test if a valid hex returns a valid color
    /// </summary>
    /// <param name="hex">Hex value</param>
    /// <param name="a">Alpha channel</param>
    /// <param name="r">Red channel</param>
    /// <param name="g">Green channel</param>
    /// <param name="b">Blue channel</param>
    [Theory]
    [InlineData("#FFFF0000", 255, 255, 0, 0)]   // Red with #
    [InlineData("FFFF0000", 255, 255, 0, 0)]    // Red without #
    [InlineData("#FF00FF00", 255, 0, 255, 0)]   // Green
    [InlineData("#80112233", 128, 17, 34, 51)]  // Custom semi-transparent
    public void HexToColor_ValidHex_ReturnsExactColor(string hex, byte a, byte r, byte g, byte b)
    {
        Color expected = Color.FromArgb(a, r, g, b);
        Color actual = ItemMapper.HexToColor(hex);

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Test if an invalid hex returns black color
    /// </summary>
    /// <param name="invalidHex">Invalid hex</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void HexToColor_NullOrWhitespace_ReturnsBlack(string? invalidHex)
    {
        Color expected = Color.FromArgb(255, 0, 0, 0);
        Color result = ItemMapper.HexToColor(invalidHex!);

        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Test if a malformed hex returns black color
    /// </summary>
    /// <param name="malformedHex"></param>
    [Theory]
    [InlineData("#FFF")]        // Too short (3 characters)
    [InlineData("#FFFF")]       // Too short (4 characters)
    [InlineData("#FFFF000011")] // Too long (10 characters)
    [InlineData("ZZZZZZZZ")]    // 8 chars, but non-hex
    [InlineData("#1234567G")]   // 'G' is not a hex digit
    public void HexToColor_MalformedHex_FallsBackToBlack(string malformedHex)
    {
        Color expected = Color.FromArgb(255, 0, 0, 0);
        Color result = ItemMapper.HexToColor(malformedHex);

        Assert.Equal(expected, result);
    }
}
