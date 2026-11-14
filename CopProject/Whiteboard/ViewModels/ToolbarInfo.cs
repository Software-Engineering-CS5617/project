/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Windows.UI;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the currently selected toolbar options
/// </summary>
public class ToolbarInfo
{
    public Color BorderColor { get; set; }
    public double BorderWidth { get; set; }
    public Color FillColor { get; set; }

    public ToolbarInfo(Color borderColor, double borderWidth, Color fillColor)
    {
        BorderColor = borderColor;
        BorderWidth = borderWidth;
        FillColor = fillColor;
    }
}
