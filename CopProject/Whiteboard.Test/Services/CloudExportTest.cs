using System.Text.Json;
using Cloud.Persistence;
using Microsoft.UI.Xaml;
using Whiteboard.Interfaces;
using Whiteboard.Services;
using Whiteboard.Models;

namespace Whiteboard.Test.Services;

public class CloudExportIntegrationTests
{
    [Fact(Skip = "Skipping test because COSMOS_KEY is not set.")]
    public async Task ExportToCloudAsync_StoreExportData()
    {
        var exportData = new CanvasExportData
        {
            ImageData = [1, 2, 3, 4],
            ContentType = "image/png",
            FileName = $"test-{Guid.NewGuid():N}.png",
            PixelWidth = 10,
            PixelHeight = 20,
            ExportDateTime = DateTime.UtcNow
        };

        var canvasRender = new TempCanvasRender(exportData);
        var cloudExport = new CloudExport(canvasRender);

        string userId = $"test-user-{Guid.NewGuid():N}";
        string whiteboardId = $"test-board-{Guid.NewGuid():N}";
        string key = $"{userId}:{whiteboardId}:{exportData.FileName}";

        IStorage storage = await StorageFactory.CreateAsync(ModuleType.WhiteBoard);

        try
        {
            bool saved = await cloudExport.SaveExportDataToCloudAsync(exportData, whiteboardId, userId);

            Assert.True(saved);

            (bool Success, byte[] Data) result = await storage.Read(key);
            Assert.True(result.Success);

            using JsonDocument document = JsonDocument.Parse(result.Data);
            JsonElement root = document.RootElement;

            Assert.Equal(whiteboardId, root.GetProperty("BoardId").GetString());
            Assert.Equal(userId, root.GetProperty("OwnerId").GetString());
            Assert.Equal(exportData.FileName, root.GetProperty("FileName").GetString());
            Assert.Equal("image/png", root.GetProperty("ContentType").GetString());
            Assert.Equal("10x20", root.GetProperty("Dimensions").GetString());
            Assert.Equal(exportData.ImageData, root.GetProperty("ImageData").GetBytesFromBase64());
        }
        finally
        {
            await storage.Delete(key);
        }
    }

    private sealed class TempCanvasRender(CanvasExportData exportData) : ICanvasRender
    {
        public Task<byte[]> RenderToPngAsync(UIElement element) =>
            Task.FromResult(exportData.ImageData);

        public Task<byte[]> RenderToJpegAsync(UIElement element) =>
            Task.FromResult(exportData.ImageData);

        public Task<CanvasExportData> CreateExportDataAsync(UIElement element, string whiteboardId, bool isJpeg = false) =>
            Task.FromResult(exportData);
    }
}
