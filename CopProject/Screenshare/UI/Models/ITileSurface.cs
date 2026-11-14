using Microsoft.UI.Xaml.Media;

namespace ScreenShare.Models;

/// <summary>
/// the picture a tile draws its frames into. the app uses a WriteableBitmap;
/// tests use a fake, since a WriteableBitmap needs a running WinUI app.
/// </summary>
internal interface ITileSurface
{
    /// <summary>gets the width in pixels</summary>
    int PixelWidth { get; }

    /// <summary>gets the height in pixels</summary>
    int PixelHeight { get; }

    /// <summary>gets what the UI binds to</summary>
    ImageSource? Source { get; }

    /// <summary>replaces the picture with raw BGRA pixels of the same size and redraws it</summary>
    void WritePixels(byte[] pixels);
}
