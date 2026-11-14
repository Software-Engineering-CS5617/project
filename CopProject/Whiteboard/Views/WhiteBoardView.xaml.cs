// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using Whiteboard.ViewModels;
using Windows.Foundation;
using Windows.UI;

namespace Whiteboard.Views;

[ExcludeFromCodeCoverage]
public sealed partial class WhiteBoardView : UserControl
{
    private WhiteBoardViewModel? _viewModel;
    private readonly ToolbarInfo _toolbarInfo = new(Colors.Black, 3, Colors.Black);

    /// <summary>
    /// Gets or sets the view model bound to this whiteboard view.
    /// </summary>
    public WhiteBoardViewModel? ViewModel
    {
        get => _viewModel;
        set {
            if (ReferenceEquals(_viewModel, value))
            {
                return;
            }

            _viewModel?.HistoryChanged -= UpdateHistoryButtons;

            _viewModel = value;

            if (_viewModel is not null)
            {
                _viewModel.InitializeViewModel(WhiteboardCanvas, _toolbarInfo);
                _viewModel.HistoryChanged += UpdateHistoryButtons;
                UpdateHistoryButtons();
            }
            else
            {
                WhiteboardCanvas.Children.Clear();
                UpdateHistoryButtons();
            }
        }
    }

    public WhiteBoardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public WhiteBoardView(WhiteBoardViewModel viewModel) : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
    }

    /// <summary>
    /// Binds a view model to the view and initializes canvas and toolbar listeners.
    /// </summary>
    public void SetViewModel(WhiteBoardViewModel viewModel)
    {
        ViewModel = viewModel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.HistoryChanged += UpdateHistoryButtons;
            UpdateHistoryButtons();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _viewModel?.HistoryChanged -= UpdateHistoryButtons;
    }

    /// <summary>
    /// Updates undo and redo availability on the UI thread.
    /// </summary>
    private void UpdateHistoryButtons()
    {
        // Network notifications arrive outside the UI thread.
        DispatcherQueue.TryEnqueue(() => {
            ButtonUndo.IsEnabled = _viewModel?.CanUndo ?? false;
            ButtonRedo.IsEnabled = _viewModel?.CanRedo ?? false;
        });
    }


    /// <summary>
    /// Requests undo of the current user's latest available action.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">Event details.</param>
    private void ButtonUndo_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.Undo();
    }

    /// <summary>
    /// Requests redo of the current user's latest available undone action.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">Event details.</param>
    private void ButtonRedo_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.Redo();
    }


    private void ButtonPen_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Pen);
    }

    private void ButtonSquare_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Square);

    }

    private void ButtonCircle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Circle);
    }

    private void ButtonEqTriangle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.EqTriangle);
    }

    private void ButtonEraseItems_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Eraser);
    }

    private void CanvasContainer_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        WhiteboardCanvas.Width = e.NewSize.Width;
        WhiteboardCanvas.Height = e.NewSize.Height;
    }

    private void WhiteboardCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        // Attempt to capture the pointer
        if (WhiteboardCanvas.CapturePointer(e.Pointer))
        {
            // If successful, get the current pointer position
            Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

            // Pass the pointer position to the view model for computation
            _viewModel.PointerPressed(new Point2D(p.X, p.Y));

            // Mark the event as handled
            e.Handled = true;
        }
    }

    private void WhiteboardCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        // Get the current pointer position
        Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

        // Pass the pointer position to the view model for computation
        _viewModel.PointerMoved(new Point2D(p.X, p.Y));

        // Mark the event as handled
        e.Handled = true;
    }

    private void WhiteboardCanvas_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        // Get the current pointer position
        Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

        // Pass the pointer position to the view model for computation
        _viewModel.PointerReleased(new Point2D(p.X, p.Y));

        // Release the pointer, if captured
        WhiteboardCanvas.ReleasePointerCapture(e.Pointer);

        // Mark the event as handled
        e.Handled = true;
    }

    private void WhiteboardCanvas_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        // Get the current pointer position
        Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

        // Pass the pointer position to the view model for computation
        _viewModel.PointerCaptureLost(new Point2D(p.X, p.Y));
    }

    /// <summary>
    /// Called when the user clicks one of the fill color options
    /// </summary>
    private void FillColorOption_Click(object sender, RoutedEventArgs e)
    {
        // Extract the tag message from the button
        if (sender is Button { Tag: string hex } && TryParseHexColor(hex, out Color color))
        {
            _toolbarInfo.FillColor = color;

            FillColorIndicator.Background = new SolidColorBrush(color);

            FlyoutFillColor.Hide();
        }
    }

    /// <summary>
    /// Called when the user clicks one of the border color options
    /// </summary>
    private void BorderColorOption_Click(object sender, RoutedEventArgs e)
    {
        // Extract the tag message from the button
        if (sender is Button { Tag: string hex } && TryParseHexColor(hex, out Color color))
        {
            _toolbarInfo.BorderColor = color;

            BorderColorIndicator.Background = new SolidColorBrush(color);

            FlyoutBorderColor.Hide();
        }
    }

    /// <summary>
    /// Called when the user moves the border width slider
    /// </summary>
    private void SliderBorderWidth_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_toolbarInfo is null)
        {
            return;
        }

        double width = Math.Round(e.NewValue);

        _toolbarInfo.BorderWidth = width;

        // Update width text next to the slider
        TextBorderWidth?.Text = $"{width}";
    }

    /// <summary>
    /// Attempt parsing a 6 digit or 8 digit hex color code
    /// </summary>
    private static bool TryParseHexColor(string hex, out Color color)
    {
        color = Colors.Black;

        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            color = Color.FromArgb(255, r, g, b);
            return true;
        }
        else if (hex.Length == 8)
        {
            byte a = Convert.ToByte(hex[..2], 16);
            byte r = Convert.ToByte(hex.Substring(2, 2), 16);
            byte g = Convert.ToByte(hex.Substring(4, 2), 16);
            byte b = Convert.ToByte(hex.Substring(6, 2), 16);
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        return false;
    }
}
