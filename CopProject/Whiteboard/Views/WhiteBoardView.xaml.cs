/// Owner   : Raagam Hitesh Parmar, Sasi Kiran, Sangam,    Karthik Krishna M, Vignesh
/// Roll No : 112301024,            112301033,  112201006, 112301014,         112301034
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using Whiteboard.ViewModels;
using Whiteboard.Models;
using Windows.Foundation;
using Windows.UI;

namespace Whiteboard.Views;

[ExcludeFromCodeCoverage]
public sealed partial class WhiteBoardView : UserControl
{
    private WhiteBoardViewModel? _viewModel;
    private readonly ToolbarInfo _toolbarInfo = new(Colors.Black, 3, Colors.Black);

    private bool _isPanning;
    private bool _isSpaceKeyDown;
    private Point _panStartPoint;
    private double _panStartHOffset;
    private double _panStartVOffset;
    private bool _isUpdatingZoomFromSlider;
    private bool _isUpdatingZoomFromScrollViewer;

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
                if (ButtonSmartShape is not null)
                {
                    ButtonSmartShape.IsChecked = _viewModel.IsSmartShapeEnabled;
                }
            }
            else
            {
                WhiteboardCanvas.Children.Clear();
                UpdateHistoryButtons();
                if (ButtonSmartShape is not null)
                {
                    ButtonSmartShape.IsChecked = false;
                }
            }
        }
    }

    public WhiteBoardView()
    {
        InitializeComponent();

        // TextBox controls mark pointer press/release as handled, so the canvas must
        // opt in to handled events; otherwise tools like the eraser never see them.
        WhiteboardCanvas.AddHandler(PointerPressedEvent, new PointerEventHandler(WhiteboardCanvas_PointerPressed), handledEventsToo: true);
        WhiteboardCanvas.AddHandler(PointerReleasedEvent, new PointerEventHandler(WhiteboardCanvas_PointerReleased), handledEventsToo: true);

        // Needed so dragging a textbox with the select tool keeps receiving moves
        WhiteboardCanvas.AddHandler(PointerMovedEvent, new PointerEventHandler(WhiteboardCanvas_PointerMoved), handledEventsToo: true);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
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

        // Center the viewport on the large canvas initially
        DispatcherQueue.TryEnqueue(() => {
            if (CanvasScrollViewer is not null)
            {
                double initialH = (WhiteboardCanvas.Width - CanvasScrollViewer.ActualWidth) / 2.0;
                double initialV = (WhiteboardCanvas.Height - CanvasScrollViewer.ActualHeight) / 2.0;
                if (initialH > 0 || initialV > 0)
                {
                    CanvasScrollViewer.ChangeView(Math.Max(0, initialH), Math.Max(0, initialV), 1.0f, true);
                }
            }
        });
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

    private void ButtonSmartShape_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is not null && sender is ToggleButton toggle)
        {
            _viewModel.IsSmartShapeEnabled = toggle.IsChecked ?? false;
        }
    }

    private void ButtonSquare_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Square);

    }

    private void ButtonRectangle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Rectangle);
    }

    private void ButtonCircle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Circle);
    }

    private void ButtonEllipse_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Ellipse);
    }

    private void ButtonEqTriangle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.EqTriangle);
    }

    private void ButtonLine_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Line);
    }

    private void ButtonPolygon_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Polygon);
    }

    private void ButtonTextBox_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Textbox);
    }

    private void ButtonEraseItems_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Eraser);
    }

    private void ButtonSelectItems_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.SelectTool(WhiteboardTool.Select);
    }

    private void WhiteboardCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        PointerPointProperties pointProps = e.GetCurrentPoint(WhiteboardCanvas).Properties;

        // Check if panning with Middle Click or with Spacebar + Click
        if (pointProps.IsMiddleButtonPressed || _isSpaceKeyDown)
        {
            if (CanvasScrollViewer is not null && WhiteboardCanvas.CapturePointer(e.Pointer))
            {
                _isPanning = true;

                _panStartPoint = e.GetCurrentPoint(CanvasScrollViewer).Position;
                _panStartHOffset = CanvasScrollViewer.HorizontalOffset;
                _panStartVOffset = CanvasScrollViewer.VerticalOffset;

                e.Handled = true;
            }
            return;
        }

        if (_viewModel is null)
        {
            return;
        }

        bool isOnTextbox = IsInsideTextbox(e.OriginalSource as DependencyObject);

        // Let the textbox keep the press when the current tool wants to type into it
        if (isOnTextbox && !_viewModel.HandlesPointerOnTextbox)
        {
            return;
        }

        // Attempt to capture the pointer. A textbox may already hold the capture,
        // in which case the press is still forwarded (e.g. so the eraser works).
        if (WhiteboardCanvas.CapturePointer(e.Pointer) || isOnTextbox)
        {
            // Get the current pointer position
            Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

            // Pass the pointer position to the view model for computation
            _viewModel.PointerPressed(new Point2D(p.X, p.Y));

            // Mark the event as handled
            e.Handled = true;
        }
    }

    /// <summary>
    /// Walks up the visual tree to check whether an element belongs to a TextBox on the canvas.
    /// </summary>
    private bool IsInsideTextbox(DependencyObject? element)
    {
        while (element is not null && element != WhiteboardCanvas)
        {
            if (element is TextBox)
            {
                return true;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return false;
    }

    private void WhiteboardCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            if (CanvasScrollViewer is not null)
            {
                Point currentPos = e.GetCurrentPoint(CanvasScrollViewer).Position;

                double deltaX = currentPos.X - _panStartPoint.X;
                double deltaY = currentPos.Y - _panStartPoint.Y;

                CanvasScrollViewer.ChangeView(_panStartHOffset - deltaX, _panStartVOffset - deltaY, null, true);

                e.Handled = true;
            }
            return;
        }

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
        if (_isPanning)
        {
            _isPanning = false;
            WhiteboardCanvas.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
            return;
        }

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
        if (_isPanning)
        {
            _isPanning = false;
            return;
        }

        // A TextBox captures the pointer itself and releases it on pointer up.
        // That capture-lost bubbles up here before PointerReleased does, and would
        // be mistaken for an interrupted drag (snapping a dragged textbox back).
        // Only the canvas losing its own capture matters.
        if (IsInsideTextbox(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (_viewModel is null)
        {
            return;
        }

        // Get the current pointer position
        Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

        // Pass the pointer position to the view model for computation
        _viewModel.PointerCaptureLost(new Point2D(p.X, p.Y));
    }

    private void WhiteboardCanvas_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        // Get the current pointer position
        Point p = e.GetPosition(WhiteboardCanvas);

        // Pass the pointer position to the view model for computation
        _viewModel.PointerDoubleTapped(new Point2D(p.X, p.Y));
    }

    private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // On pressing the Escape key, reset the state machine
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            _viewModel?.ResetCurrentState();

            // Mark the event as handled
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Space)
        {
            _isSpaceKeyDown = true;
        }
    }

    private void OnKeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Space)
        {
            _isSpaceKeyDown = false;
        }
    }

    private void CanvasScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isUpdatingZoomFromSlider || CanvasScrollViewer is null)
        {
            return;
        }

        float factor = CanvasScrollViewer.ZoomFactor;
        int zoomPercent = (int) Math.Round(factor * 100);

        _isUpdatingZoomFromScrollViewer = true;
        try
        {
            SliderZoom?.Value = zoomPercent;
            TextZoom?.Text = $"{zoomPercent}%";
        }
        finally
        {
            _isUpdatingZoomFromScrollViewer = false;
        }
    }

    private void SliderZoom_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingZoomFromScrollViewer || CanvasScrollViewer is null)
        {
            return;
        }

        int zoomPercent = (int)Math.Round(e.NewValue);
        TextZoom?.Text = $"{zoomPercent}%";

        _isUpdatingZoomFromSlider = true;
        try
        {
            CanvasScrollViewer.ChangeView(null, null, (float)(zoomPercent / 100.0f));
        }
        finally
        {
            _isUpdatingZoomFromSlider = false;
        }
    }

    private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
    {
        SliderZoom?.Value = Math.Min(SliderZoom.Maximum, SliderZoom.Value + 10);
    }

    private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
    {
        SliderZoom?.Value = Math.Max(SliderZoom.Minimum, SliderZoom.Value - 10);
    }

    private void ButtonResetZoom_Click(object sender, RoutedEventArgs e)
    {
        SliderZoom?.Value = 100;
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

    private async void ButtonSaveSnapshot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.SaveSnapshotAsync();
            await ShowMessageAsync("Saved", "Whiteboard snapshot saved to Cloud");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Save failed", ex.Message);
        }
    }

    private async void ButtonRestoreSnapshot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            List<WhiteboardSnapshotSummary> snapshots = await _viewModel.ListSnapshotsAsync();

            if (snapshots.Count == 0)
            {
                await ShowMessageAsync("No snapshots", "No saved snapshots for this session");
                return;
            }

            var flyout = new MenuFlyout();

            foreach (WhiteboardSnapshotSummary snapshot in snapshots)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = snapshot.ToString()
                };

                menuItem.Click += async (_, _) =>
                {
                    try
                    {
                        await _viewModel.RestoreSnapshotAsync(snapshot.SnapshotId);
                    }
                    catch (Exception ex)
                    {
                        await ShowMessageAsync("Restore failed", ex.Message);
                    }
                };

                flyout.Items.Add(menuItem);
            }

            flyout.ShowAt(ButtonRestoreSnapshot);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Could not load snapshots", ex.Message);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}
