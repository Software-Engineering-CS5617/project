using System;

namespace Whiteboard.Interfaces;

public class CanvasExportData
{
    public byte[] ImageData { get; set; }
    public string ContentType { get; set; }
    public string FileName { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public DateTime ExportDateTime { get; set; }
}