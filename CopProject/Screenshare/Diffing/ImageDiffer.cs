using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenShare;

public class ImageDiffer : IImageDiffer
{
    /// <summary>width and height of a full tile in pixels, edge tiles may be smaller</summary>
    public const int TILE_SIZE = 64;

    /*if more than this percentage of the screen has changed, sending individual tiles becomes less useful, so we send the complete frame.*/
    private const double FULL_FRAME_THRESHOLD = 0.50;

    private const ulong FNV_OFFSET_BASIS = 14695981039346656037UL;
    private const ulong FNV_PRIME = 1099511628211UL;

    //private readonly Dictionary<(int X, int Y), ulong> previousTileHashes;

    //public ImageDiffer()
    //{
    //    previousTileHashes =
    //        new Dictionary<(int X, int Y), ulong>();
    //}

    public ImageDiffResult Compare(Bitmap previousFrame, Bitmap currentFrame)
    {
        if (previousFrame.Width != currentFrame.Width ||
            previousFrame.Height != currentFrame.Height)
        {
            return ImageDiffResult.CreateFullFrame(new Bitmap(currentFrame));
        }

        int columns = (currentFrame.Width + TILE_SIZE - 1) / TILE_SIZE;

        int rows = (currentFrame.Height + TILE_SIZE - 1) / TILE_SIZE;

        List<Tile> changedTiles = new();

        long totalPixels = (long)currentFrame.Width * currentFrame.Height;

        long changedPixels = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int x = column * TILE_SIZE;
                int y = row * TILE_SIZE;

                int width = Math.Min(
                    TILE_SIZE,
                    currentFrame.Width - x
                );

                int height = Math.Min(
                    TILE_SIZE,
                    currentFrame.Height - y
                );

                ulong previousHash = CalculateTileHash(
                    previousFrame,
                    x,
                    y,
                    width,
                    height
                );

                ulong currentHash = CalculateTileHash(
                    currentFrame,
                    x,
                    y,
                    width,
                    height
                );

                if (previousHash != currentHash)
                {
                    byte[] tileData = ExtractTileData(
                        currentFrame,
                        x,
                        y,
                        width,
                        height
                    );

                    changedTiles.Add(
                        new Tile(
                            x,
                            y,
                            width,
                            height,
                            currentHash,
                            tileData
                        )
                    );

                    changedPixels += (long)width * height;
                }
            }
        }

        // Nothing changed.

        if (changedTiles.Count == 0)
        {
            return ImageDiffResult.Unchanged();
        }

        //tooo much of the screen changed.
        //send a full frame instead of many tiles.

        double changeRatio =
            (double)changedPixels / totalPixels;

        if (changeRatio >= FULL_FRAME_THRESHOLD)
        {
            return ImageDiffResult.CreateFullFrame(
                new Bitmap(currentFrame)
            );
        }

        // only a small part changed. send the changed tiles.

        return ImageDiffResult.Delta(changedTiles);
    }

    private ulong CalculateTileHash(
        Bitmap bitmap,
        int startX,
        int startY,
        int width,
        int height)
    {

        Rectangle rectangle = new(
            startX,
            startY,
            width,
            height
            );

        /// bitmap represents image while bitmapData represents locked pixel memory including Scan0 and Stride
        BitmapData data = bitmap.LockBits(
            rectangle,
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb
            );


        try
        {
            ulong hash = FNV_OFFSET_BASIS;
            int bytesPerPixel = 4;
            int rowBytes = width * bytesPerPixel;
            byte[] buffer = new byte[rowBytes];

            for (int y = 0; y < height; y++)
            {
                IntPtr rowPointer = IntPtr.Add(
                    data.Scan0,
                    y * data.Stride
                    );

                Marshal.Copy(
                    rowPointer,
                    buffer,
                    0,
                    rowBytes
                    );
                for (int i = 0; i < rowBytes; i++)
                {
                    hash ^= buffer[i];
                    hash *= FNV_PRIME;
                }
            }
            return hash;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    //private List<Tile> FindChangedTiles(Bitmap currentFrame)
    //{
    //    List<Tile> changedTiles = new List<Tile>();

    //    int screenWidth = currentFrame.Width;
    //    int screenHeight = currentFrame.Height;

    //    for (int y = 0; y < screenHeight; y += TILE_SIZE)
    //    {
    //        for (int x = 0; x < screenWidth; x += TILE_SIZE)
    //        {
    //            int tileWidth = Math.Min(
    //                TILE_SIZE,
    //                screenWidth - x
    //            );

    //            int tileHeight = Math.Min(
    //                TILE_SIZE,
    //                screenHeight - y
    //            );

    //            ulong currentHash = CalculateTileHash(
    //                currentFrame,
    //                x,
    //                y,
    //                tileWidth,
    //                tileHeight
    //            );

    //            var tilePosition = (x, y);

    //            bool changed = true;

    //            if (_previousTileHashes.TryGetValue(
    //                    tilePosition,
    //                    out ulong previousHash))
    //            {
    //                if (previousHash != currentHash)
    //                {
    //                    Console.WriteLine(
    //                        $"Changed tile ({x}, {y}) | " +
    //                        $"Old: {previousHash:X16} | " +
    //                        $"New: {currentHash:X16}"
    //                    );
    //                }
    //                changed = previousHash != currentHash;
    //            }

    //            if (changed)
    //            {
    //                Tile tile = new Tile {
    //                    X = x,
    //                    Y = y,
    //                    Width = tileWidth,
    //                    Height = tileHeight,
    //                    Hash = currentHash,
    //                    Data = ExtractTileData(currentFrame, x, y, tileWidth, tileHeight)
    //                };

    //                changedTiles.Add(tile);
    //            }

    //            _previousTileHashes[tilePosition] = currentHash;
    //        }
    //    }

    //    return changedTiles;
    //}

    private byte[] ExtractTileData(
        Bitmap bitmap,
        int startX,
        int startY,
        int width,
        int height)
    {

        Rectangle rectangle = new(
            startX,
            startY,
            width,
            height
            );


        using Bitmap tileBitmap = bitmap.Clone(
            rectangle,
            PixelFormat.Format32bppArgb);

        return PngEncoder.Encode(tileBitmap);
    }
}
