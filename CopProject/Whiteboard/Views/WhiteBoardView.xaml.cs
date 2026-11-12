// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Whiteboard.ViewModels;

namespace Whiteboard.Views;

public sealed partial class WhiteBoardView : UserControl
{
    private readonly WhiteBoardViewModel _viewModel;

    public WhiteBoardView(WhiteBoardViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        _viewModel = viewModel;

        ToolbarInfo toolbarInfo = new(new SolidColorBrush(Colors.Black), 3, new SolidColorBrush(Colors.Red));
        _viewModel.InitializeViewModel(WhiteboardCanvas, toolbarInfo);
    }

    private void ButtonPen_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Pen);
    }

    private void ButtonSquare_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Square);

    }

    private void ButtonCircle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Circle);
    }

    private void ButtonEqTriangle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.EqTriangle);
    }

    private void ButtonEllipse_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Ellipse);
    }

    private void ButtonEraseItems_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Eraser);
    }

    private void CanvasContainer_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        WhiteboardCanvas.Width = e.NewSize.Width;
        WhiteboardCanvas.Height = e.NewSize.Height;
    }

    private void WhiteboardCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _viewModel.PointerPressed(sender, e);
    }

    private void WhiteboardCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _viewModel.PointerMoved(sender, e);
    }

    private void WhiteboardCanvas_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _viewModel.PointerReleased(sender, e);
    }

    private void WhiteboardCanvas_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _viewModel.PointerCaptureLost(sender, e);
    }
}
