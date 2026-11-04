using WhiteboardSuggestion.Core;
using Xunit;

namespace WhiteboardSuggestion.Tests;

public sealed class ImageMimeTypeResolverTests
{
    [Theory]
    [InlineData("drawing.png", "image/png")]
    [InlineData("circle.JPG", "image/jpeg")]
    [InlineData("letter.jpeg", "image/jpeg")]
    [InlineData("digit.webp", "image/webp")]
    public void GetMimeType_KnownExtension_ReturnsMimeType(string filePath, string expected)
    {
        Assert.Equal(expected, ImageMimeTypeResolver.GetMimeType(filePath));
    }

    [Fact]
    public void GetMimeType_UnsupportedExtension_Throws()
    {
        Assert.Throws<NotSupportedException>(() => ImageMimeTypeResolver.GetMimeType("drawing.pdf"));
    }

    [Fact]
    public void GetMimeType_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => ImageMimeTypeResolver.GetMimeType(" "));
    }
}
