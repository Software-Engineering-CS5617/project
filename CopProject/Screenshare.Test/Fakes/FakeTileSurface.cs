using Microsoft.UI.Xaml.Media;
using ScreenShare.Models;

namespace Screenshare.Tests.Fakes;

/// <summary>
/// stands in for the WriteableBitmap a tile draws into, which needs a running WinUI app.
/// it only records the pixels it was given, so a test can check them.
/// </summary>
internal sealed class FakeTileSurface : ITileSurface
{
    public FakeTileSurface(int width, int height)
    {
        PixelWidth = width;
        PixelHeight = height;
    }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    // a real ImageSource cannot be made without WinUI 
    public ImageSource? Source => null;

    /// <summary>every WritePixels call, in order</summary>
    public List<byte[]> Writes { get; } = new();

    /// <summary>when true, WritePixels throws, like a bitmap that is no longer usable</summary>
    public bool FailWrites { get; set; }

    public void WritePixels(byte[] pixels)
    {
        if (FailWrites)
        {
            throw new InvalidOperationException("surface is broken");
        }

        Writes.Add(pixels);
    }
}
