// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Drawing.Imaging;
using ScreenShare;

namespace Screenshare.Tests;

public class PngEncoderTests
{
    [Fact]
    public void Encode_ValidBitmap_ReturnsPngBytes()
    {
        using var bitmap = new Bitmap(10, 10);
        bitmap.SetPixel(5, 5, Color.Red);

        byte[] encoded = PngEncoder.Encode(bitmap);

        // ensure bytes are non-empty and represent a PNG
        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);

        // png files start with this 8byte signature
        byte[] pngSignature =
        {
            137, 80, 78, 71, 13, 10, 26, 10
        };

        Assert.Equal(
            pngSignature,
            encoded.Take(pngSignature.Length).ToArray());

        // decode the output to verify it is a valid image
        using var stream = new MemoryStream(encoded);
        using var decoded = new Bitmap(stream);

        Assert.Equal(10, decoded.Width);
        Assert.Equal(10, decoded.Height);
        Assert.Equal(Color.Red.ToArgb(), decoded.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void Encode_NullBitmap_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => PngEncoder.Encode(null!));
    }

    [Fact]
    public void FindPngCodec_PngAvailableImmediately_ReturnsWithoutSleeping()
    {
        var sleeps = new List<int>();

        ImageCodecInfo codec = PngEncoder.FindPngCodec(
            ImageCodecInfo.GetImageEncoders,
            sleeps.Add);

        Assert.Equal(ImageFormat.Png.Guid, codec.FormatID);
        Assert.Empty(sleeps);
    }

    [Fact]
    public void FindPngCodec_PngMissingAtFirst_RetriesUntilFound()
    {
        // simulates GDI+ still building its codec list: the first two lookups
        // come back without png, the third returns the full list
        int calls = 0;
        var sleeps = new List<int>();

        ImageCodecInfo codec = PngEncoder.FindPngCodec(
            () => ++calls < 3 ? EncodersWithoutPng() : ImageCodecInfo.GetImageEncoders(),
            sleeps.Add);

        Assert.Equal(ImageFormat.Png.Guid, codec.FormatID);
        Assert.Equal(3, calls);
        Assert.Equal(new[] { 10, 20 }, sleeps);
    }

    [Fact]
    public void FindPngCodec_PngNeverAvailable_ThrowsAfterAllAttempts()
    {
        int calls = 0;
        var sleeps = new List<int>();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PngEncoder.FindPngCodec(
                () => {
                    calls++;
                    return EncodersWithoutPng();
                },
                sleeps.Add));

        Assert.Equal("GDI+ has no PNG encoder", ex.Message);
        Assert.Equal(5, calls);
        Assert.Equal(new[] { 10, 20, 30, 40, 50 }, sleeps);
    }

    [Fact]
    public void FindPngCodec_EmptyEncoderList_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => PngEncoder.FindPngCodec(
                () => Array.Empty<ImageCodecInfo>(),
                _ => { }));
    }

    private static ImageCodecInfo[] EncodersWithoutPng()
    {
        return ImageCodecInfo.GetImageEncoders()
            .Where(c => c.FormatID != ImageFormat.Png.Guid)
            .ToArray();
    }
}
