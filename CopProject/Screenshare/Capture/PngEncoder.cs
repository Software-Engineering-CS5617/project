using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenShare;

/// <summary>
/// encodes bitmaps as PNG from any thread.
///
/// Image.Save(stream, ImageFormat.Png) looks the PNG encoder up in GDI+ codec list on every
/// call. when several threads do that while GDI+ is still building the list, one of them can
/// get an incomplete list and Save throws "Value cannot be null".
/// looking the encoder up once, in a static constructor that the runtime runs on one thread avoids that race.
/// </summary>
public static class PngEncoder
{
    private const int LookupAttempts = 5;

    private static readonly ImageCodecInfo s_codec =
        FindPngCodec(ImageCodecInfo.GetImageEncoders, Thread.Sleep);

    /// <summary>
    /// encodes the bitmap as png bytes
    /// </summary>
    public static byte[] Encode(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var stream = new MemoryStream();
        bitmap.Save(stream, s_codec, null);
        return stream.ToArray();
    }

    /// <summary>
    /// looks up the png encoder, retrying with a growing delay while the codec list is incomplete.
    /// the encoder source and the sleep are parameters so tests can simulate a list without png.
    /// </summary>
    /// <param name="getEncoders">returns the current GDI+ encoder list</param>
    /// <param name="sleep">waits for the given number of milliseconds between attempts</param>
    internal static ImageCodecInfo FindPngCodec(
        Func<ImageCodecInfo[]> getEncoders,
        Action<int> sleep
        )
    {
        for (int attempt = 1; attempt <= LookupAttempts; attempt++)
        {
            ImageCodecInfo? codec = getEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Png.Guid);

            if (codec != null)
            {
                return codec;
            }

            // another thread may still be initializing GDI+ codec list
            sleep(10 * attempt);
        }

        throw new InvalidOperationException("GDI+ has no PNG encoder");
    }
}
