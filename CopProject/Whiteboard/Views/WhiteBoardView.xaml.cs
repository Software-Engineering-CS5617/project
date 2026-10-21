using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Whiteboard.Items;

namespace Whiteboard.Views;

public sealed partial class WhiteBoardView : UserControl
{
    private readonly WhiteboardCanvas _canvas;

    public WhiteBoardView()
    {
        InitializeComponent();

        _canvas = new WhiteboardCanvas();
    }

    private void TextButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO

        //var textItem = new Textbox {
        //    Text = "New Text",
        //    X = 50,
        //    Y = 50,
        //    Width = 150,
        //    Height = 40
        //};

        //_canvas.AddItem(textItem);

        //var textBlock = new TextBlock {
        //    Text = textItem.Text,
        //    Width = textItem.Width,
        //    Height = textItem.Height,
        //    FontSize = 20
        //};

        //Canvas.SetLeft(textBlock, textItem.X);
        //Canvas.SetTop(textBlock, textItem.Y);

        //WhiteboardCanvas.Children.Add(textBlock);
    }
}
