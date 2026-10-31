using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Whiteboard.Interfaces;

public interface ICanvasRender
{
    /// <summary>
    /// Converts UI element to PNG byte data 
    /// </summary>
    Task<byte> RenderToPngAsync(UIElement element);

    /// <summary>
    /// Converts UI element to JPEG byte data 
    /// </summary>
    Task<byte> RenderToJpegAsync(UIElement element);
}