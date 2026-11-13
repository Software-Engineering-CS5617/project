using System.Drawing;
using ScreenShare.Models;

namespace Screenshare.Tests;

public class ScreenFrameTests
{

    [Fact]
    public void Constructor_StoresValues()
    {
        byte[] pixels = new byte[3 * 2 * 4];

        ScreenFrame frame = new(3, 2, pixels);

        Assert.Equal(3, frame.Width);
        Assert.Equal(2, frame.Height);
        Assert.Same(pixels, frame.Pixels);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(-1, -1)]
    public void Constructor_RejectsNonPositiveSize(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScreenFrame(width, height, new byte[4]));
    }

    [Fact]
    public void Constructor_RejectsNullPixels()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenFrame(1, 1, null!));
    }

    [Theory]
    [InlineData(3 * 2 * 4 - 1)]
    [InlineData(3 * 2 * 4 + 1)]
    [InlineData(0)]
    public void Constructor_RejectsWrongPixelLength(int length)
    {
        Assert.Throws<ArgumentException>(() => new ScreenFrame(3, 2, new byte[length]));
    }


    [Fact]
    public void FromBitmap_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ScreenFrame.FromBitmap(null!));
    }

    [Fact]
    public void FromBitmap_CopiesSizeAndPixelsInBgraOrder()
    {
        using Bitmap bitmap = new(3, 2);
        bitmap.SetPixel(2, 1, Color.FromArgb(255, 10, 20, 30)); // A, R, G, B

        ScreenFrame frame = ScreenFrame.FromBitmap(bitmap);

        Assert.Equal(3, frame.Width);
        Assert.Equal(2, frame.Height);
        Assert.Equal(3 * 2 * 4, frame.Pixels.Length);

        // pixel (x=2, y=1) starts at (y * width + x) * 4 = (1 * 3 + 2) * 4 = 20
        Assert.Equal(new byte[] { 30, 20, 10, 255 }, frame.Pixels[20..24]); // B, G, R, A

        // pixel (0, 0) was never set, so it is still transparent black
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, frame.Pixels[0..4]);
    }

    [Fact]
    public void FromBitmap_PremultipliesAlpha()
    {
        using Bitmap bitmap = new(1, 1);
        bitmap.SetPixel(0, 0, Color.FromArgb(128, 200, 100, 50)); // half transparent

        ScreenFrame frame = ScreenFrame.FromBitmap(bitmap);

        // each colour becomes colour * alpha / 255; allow 1 either way for rounding
        Assert.InRange(frame.Pixels[0], 50 * 128 / 255 - 1, 50 * 128 / 255 + 1);
        Assert.InRange(frame.Pixels[1], 100 * 128 / 255 - 1, 100 * 128 / 255 + 1);
        Assert.InRange(frame.Pixels[2], 200 * 128 / 255 - 1, 200 * 128 / 255 + 1);
        Assert.Equal(128, frame.Pixels[3]);
    }
}
