using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using Windows.UI;

namespace Whiteboard.Items;

/// <inheritdoc/>
public class Textbox : Item, ITextbox
{
    // TODO
    public override Rect2D BoundingBox { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    // TODO
    public override bool HitTest(Point2D point)
    {
        throw new System.NotImplementedException();
    }

    public string Content { get; set; } = string.Empty;
    public FontFamily FontFamily { get; set; } = new FontFamily("Segoe UI");
    public int FontSize { get; set; } = 15;
    public Color FontColor { get; set; } = Colors.Black;
}
