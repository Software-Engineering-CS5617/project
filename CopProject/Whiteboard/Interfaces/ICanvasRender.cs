/// Owner   : Karthik Krishna M
/// LICENSE : MIT

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Whiteboard.Models; 

namespace Whiteboard.Interfaces;

public interface ICanvasRender
{
    /// <summary>
    /// Converts UI element to PNG byte data 
    /// </summary>
    Task<byte[]> RenderToPngAsync(UIElement element);

    /// <summary>
    /// Converts UI element to JPEG byte data 
    /// </summary>
    Task<byte[]> RenderToJpegAsync(UIElement element);

    /// <summary>
    /// Captures a UI element and generates a CanvasExportData container.
    /// </summary>
    Task<CanvasExportData> CreateExportDataAsync(UIElement element, string whiteboardId, bool isJpeg = false);
}