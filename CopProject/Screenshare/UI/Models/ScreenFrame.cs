using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenShare.Models;

/// <summary>
/// a stream's picture as raw pixels, ready to copy into a WriteableBitmap without
/// any encoding: 4 bytes per pixel in BGRA order with premultiplied alpha, rows packed
/// with no padding (stride = Width * 4).
/// </summary>
public sealed class ScreenFrame
{
    /// <summary>gets the width in pixels</summary>
    public int Width { get; }

    /// <summary>gets the height in pixels</summary>
    public int Height { get; }

    /// <summary>gets the pixel data; Width * Height * 4 bytes</summary>
    public byte[] Pixels { get; }

    /// <exception cref="ArgumentOutOfRangeException">a dimension is not positive</exception>
    /// <exception cref="ArgumentException">the pixel data does not match the dimensions</exception>
    public ScreenFrame(int width, int height, byte[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(pixels);

        if (pixels.Length != (long)width * height * 4)
        {
            throw new ArgumentException("pixel data does not match the frame size", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    /// <summary>
    /// copies a bitmap's pixels into the layout WriteableBitmap expects
    /// </summary>
    public static ScreenFrame FromBitmap(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        int width = bitmap.Width;
        int height = bitmap.Height;
        int rowBytes = width * 4;
        byte[] pixels = new byte[rowBytes * height];

        // PArgb is BGRA in memory with premultiplied alpha, which is what WriteableBitmap uses
        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppPArgb);

        try
        {
            // copy row by row, since the bitmap's stride may include padding
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * rowBytes, rowBytes);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return new ScreenFrame(width, height, pixels);
    }
}
