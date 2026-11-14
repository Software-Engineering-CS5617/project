using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Cloud.Persistence;
using Microsoft.UI.Xaml;
using Whiteboard.Interfaces;
using Whiteboard.Models;

namespace Whiteboard.Services;

[ExcludeFromCodeCoverage]
public class CloudExport
{
    private readonly ICanvasRender _canvasRender;
    private IStorage? _storage;

    public CloudExport(ICanvasRender canvasRender, IStorage? storage = null)
    {
        ArgumentNullException.ThrowIfNull(canvasRender);
        _canvasRender = canvasRender;
        _storage = storage;
    }

    public async Task<bool> ExportToCloudAsync(UIElement element, string whiteboardId, string userId, bool isJpeg = false)
    {
        ArgumentNullException.ThrowIfNull(element);

        _storage ??= await StorageFactory.CreateAsync(ModuleType.WhiteBoard);

        CanvasExportData exportData = await _canvasRender.CreateExportDataAsync(element, whiteboardId, isJpeg);

        return await SaveExportDataToCloudAsync(exportData, whiteboardId, userId);
    }

    public async Task<bool> SaveExportDataToCloudAsync(CanvasExportData exportData, string whiteboardId, string userId)
    {
        ArgumentNullException.ThrowIfNull(exportData);

        _storage ??= await StorageFactory.CreateAsync(ModuleType.WhiteBoard);

        var record = new
        {
            BoardId = whiteboardId,
            OwnerId = userId,
            exportData.FileName,
            exportData.ContentType,
            Dimensions = $"{exportData.PixelWidth}x{exportData.PixelHeight}",
            ExportedAt = exportData.ExportDateTime,
            exportData.ImageData
        };

        byte[] data = JsonSerializer.SerializeToUtf8Bytes(record);
        string key = $"{userId}:{whiteboardId}:{exportData.FileName}";

        return await _storage.Create(key, data);
    }
}