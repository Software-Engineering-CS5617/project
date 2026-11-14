/// Owner   : Karthik Krishna M
/// LICENSE : MIT

using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.Services;

// Temporary exclusion from code coverage
[ExcludeFromCodeCoverage]
public class CanvasRender : ICanvasRender
{
    public async Task<byte[]> RenderToPngAsync(UIElement element)
    {
        return await RenderToBytesAsync(element, BitmapEncoder.PngEncoderId);
    }

    public async Task<byte[]> RenderToJpegAsync(UIElement element)
    {
        return await RenderToBytesAsync(element, BitmapEncoder.JpegEncoderId);
    }

    public async Task<CanvasExportData> CreateExportDataAsync(UIElement element, string whiteboardId, bool isJpeg = false)
    {
        if(element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        var targetBitmap = new RenderTargetBitmap();
        await targetBitmap.RenderAsync(element);

        Guid encoderId = isJpeg ? BitmapEncoder.JpegEncoderId : BitmapEncoder.PngEncoderId;
        string extension = isJpeg ? "jpg" : "png";
        string contentType = isJpeg ? "image/jpeg" : "image/png";   

        byte[] bytes = await BitmapToBytesAsync(targetBitmap, encoderId);

        return new CanvasExportData
        {
            ImageData = bytes,
            ContentType = contentType,
            FileName = $"whiteboard_{whiteboardId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}",
            PixelWidth = targetBitmap.PixelWidth,
            PixelHeight = targetBitmap.PixelHeight,
            ExportDateTime = DateTime.UtcNow
        };
    }

    private async Task<byte[]> RenderToBytesAsync(UIElement element, Guid encoderId)
    {
        if(element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        var targetBitmap = new RenderTargetBitmap();
        await targetBitmap.RenderAsync(element);

        return await BitmapToBytesAsync(targetBitmap, encoderId);
    }

    private async Task<byte[]> BitmapToBytesAsync(RenderTargetBitmap bitmap, Guid encoderId)
    {
       IBuffer buffer = await bitmap.GetPixelsAsync();
       byte[] pixels = buffer.ToArray();

       using var memoryStream = new InMemoryRandomAccessStream();
       BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, memoryStream);

       encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth,
            (uint)bitmap.PixelHeight,
            96, 
            96, 
            pixels
        );

        await encoder.FlushAsync();

        byte[] imageBytes = new byte[memoryStream.Size];
        using (var reader = new DataReader(memoryStream.GetInputStreamAt(0)))
        {
            await reader.LoadAsync((uint)memoryStream.Size);
            reader.ReadBytes(imageBytes);
        }

        return imageBytes;
    }
}
