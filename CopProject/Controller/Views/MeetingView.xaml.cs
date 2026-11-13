using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Controller.ViewModels;

namespace Controller.Views;

public sealed partial class MeetingView : UserControl
{
    private MeetingViewModel? _viewModel;

    public MeetingViewModel ViewModel
    {
        get => _viewModel!;
        set
        {
            if (_viewModel != null)
            {
                _viewModel.ChartUpdated -= DrawCharts;
            }
            _viewModel = value;
            if (_viewModel != null)
            {
                _viewModel.ChartUpdated += DrawCharts;
            }
            this.Bindings.Update();
            DrawCharts();
        }
    }

    public MeetingView()
    {
        InitializeComponent();
    }

    private void DrawCharts()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (MiniChartCanvas != null && MiniChartCanvas.ActualWidth > 0)
            {
                DrawGraph(MiniChartCanvas, isBig: false);
            }
            if (BigChartCanvas != null && BigChartCanvas.ActualWidth > 0 && ViewModel?.IsGraphMaximized == true)
            {
                DrawGraph(BigChartCanvas, isBig: true);
            }
        });
    }

    private void MiniChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawCharts();
    private void BigChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawCharts();

    private void DrawGraph(Canvas canvas, bool isBig)
    {
        if (ViewModel == null) return;
        canvas.Children.Clear();

        double w = canvas.ActualWidth;
        double h = canvas.ActualHeight;
        if (w < 50 || h < 30) return;

        double left = isBig ? 45 : 30;
        double right = 15;
        double top = 15;
        double bottom = 25;
        double pw = Math.Max(10, w - left - right);
        double ph = Math.Max(10, h - top - bottom);

        double currentSec = ViewModel.ElapsedSeconds;
        var points = ViewModel.GraphPoints.ToList();
        if (points.Count == 0)
        {
            points.Add(new Models.ParticipantDataPoint { Seconds = 0, TimeLabel = "00:00", Count = Math.Max(1, ViewModel.Participants) });
        }

        double maxSec = Math.Max(60, Math.Max(currentSec, points.Max(p => p.Seconds)));
        if (maxSec <= 120) maxSec = 120;
        else if (maxSec <= 300) maxSec = 300;
        else if (maxSec <= 900) maxSec = 900;
        else if (maxSec <= 1800) maxSec = 1800;
        else maxSec = Math.Max(3600, Math.Ceiling(maxSec / 1800.0) * 1800.0);

        int maxCount = Math.Max(4, points.Max(p => p.Count) + 1);

        // Draw Axes Lines
        var xAxis = new Line { X1 = left, Y1 = top + ph, X2 = left + pw, Y2 = top + ph, Stroke = new SolidColorBrush(Colors.LightGray), StrokeThickness = 1 };
        var yAxis = new Line { X1 = left, Y1 = top, X2 = left, Y2 = top + ph, Stroke = new SolidColorBrush(Colors.LightGray), StrokeThickness = 1 };
        canvas.Children.Add(xAxis);
        canvas.Children.Add(yAxis);

        // Y Labels
        for (int y = 0; y <= maxCount; y += Math.Max(1, maxCount / 4))
        {
            double py = top + ph - (y / (double)maxCount) * ph;
            var lbl = new TextBlock { Text = y.ToString(), FontSize = 10, Foreground = new SolidColorBrush(Colors.Gray), TextAlignment = TextAlignment.Right, Width = left - 5 };
            Canvas.SetLeft(lbl, 0);
            Canvas.SetTop(lbl, py - 7);
            canvas.Children.Add(lbl);
        }

        // X Labels
        int steps = isBig ? 6 : 4;
        for (int i = 0; i <= steps; i++)
        {
            double s = (i / (double)steps) * maxSec;
            double px = left + (s / maxSec) * pw;
            int mins = (int)(s / 60);
            int sec = (int)(s % 60);
            var lbl = new TextBlock { Text = $"{mins:D2}:{sec:D2}", FontSize = 9, Foreground = new SolidColorBrush(Colors.Gray), Width = 40, TextAlignment = TextAlignment.Center };
            Canvas.SetLeft(lbl, px - 20);
            Canvas.SetTop(lbl, top + ph + 3);
            canvas.Children.Add(lbl);
        }

        // Plot Polyline
        var linePoints = new PointCollection();
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            double x = left + Math.Min(pw, (p.Seconds / maxSec) * pw);
            double y = top + ph - (p.Count / (double)maxCount) * ph;

            if (i > 0)
            {
                double prevY = top + ph - (points[i - 1].Count / (double)maxCount) * ph;
                linePoints.Add(new Point(x, prevY));
            }
            linePoints.Add(new Point(x, y));
        }

        // Extend polyline to current time
        double currentX = left + Math.Min(pw, (currentSec / maxSec) * pw);
        double currentY = linePoints.Last().Y;
        if (currentX > linePoints.Last().X)
        {
            linePoints.Add(new Point(currentX, currentY));
        }
        else if (linePoints.Count == 1)
        {
            linePoints.Add(new Point(currentX + 1, currentY));
        }

        var polyline = new Polyline
        {
            Points = linePoints,
            Stroke = new SolidColorBrush(ColorHelper.FromArgb(255, 15, 95, 194)),
            StrokeThickness = 2
        };
        canvas.Children.Add(polyline);

        // Event dots
        foreach (var p in points)
        {
            double x = left + Math.Min(pw, (p.Seconds / maxSec) * pw);
            double y = top + ph - (p.Count / (double)maxCount) * ph;
            var dot = new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 15, 95, 194)) };
            Canvas.SetLeft(dot, x - 3);
            Canvas.SetTop(dot, y - 3);
            canvas.Children.Add(dot);
        }

        // Live head dot moving at current time
        var currentDot = new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 15, 95, 194)) };
        Canvas.SetLeft(currentDot, currentX - 3);
        Canvas.SetTop(currentDot, currentY - 3);
        canvas.Children.Add(currentDot);
    }

    private void GraphCard_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel?.MaximizeGraph();
    private void MaximizeGraph_Click(object sender, RoutedEventArgs e) => ViewModel?.MaximizeGraph();
    private void AuditCard_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel?.MaximizeAudit();
    private void MaximizeAudit_Click(object sender, RoutedEventArgs e) => ViewModel?.MaximizeAudit();
    private void CloseMaximized_Click(object sender, RoutedEventArgs e) => ViewModel?.ShowNormal();
}
