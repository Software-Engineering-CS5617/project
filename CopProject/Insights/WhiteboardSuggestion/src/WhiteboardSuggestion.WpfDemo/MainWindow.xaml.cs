using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WhiteboardSuggestion.Core;
using WhiteboardSuggestion.WpfDemo.Controls;

namespace WhiteboardSuggestion.WpfDemo;

public partial class MainWindow : Window
{
    private readonly List<Polyline> _roughStrokes = [];
    private Polyline? _currentStroke;
    private IShapeSuggestionService? _suggestionService;
    private bool _isDrawing;
    private bool _isAnalyzing;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isAnalyzing)
        {
            return;
        }

        Point point = e.GetPosition(DrawingCanvas);
        _currentStroke = new Polyline
        {
            Stroke = Brushes.DarkSlateBlue,
            StrokeThickness = 4,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            SnapsToDevicePixels = true
        };
        _currentStroke.Points.Add(point);
        _roughStrokes.Add(_currentStroke);
        DrawingCanvas.Children.Add(_currentStroke);
        _isDrawing = true;
        DrawingCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing || _currentStroke is null)
        {
            return;
        }

        Point point = e.GetPosition(DrawingCanvas);
        _currentStroke.Points.Add(point);
    }

    private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        _currentStroke = null;
        DrawingCanvas.ReleaseMouseCapture();
        StatusText.Text = "Sketch ready. Click Suggest correction to recognize it.";
    }

    private async void SuggestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isAnalyzing)
        {
            return;
        }

        if (_roughStrokes.Count == 0)
        {
            StatusText.Text = "Draw a rough shape, letter, or digit first.";
            return;
        }

        string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            MessageBox.Show(
                "Set GEMINI_API_KEY before running the demo.\n\nPowerShell:\n$env:GEMINI_API_KEY = \"YOUR_API_KEY\"",
                "Gemini API key missing",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _isAnalyzing = true;
        SuggestButton.IsEnabled = false;
        StatusText.Text = "Analyzing the rough sketch…";

        try
        {
            byte[] imageBytes = CaptureRoughStrokesAsPng();
            _suggestionService ??= new GeminiShapeSuggestionService(apiKey);
            ShapeSuggestion suggestion = await _suggestionService.SuggestAsync(imageBytes, "image/png");
            SuggestionOverlay.ShowSuggestion(suggestion);
            StatusText.Text = "Choose a suggestion card. It will replace the rough sketch on the board.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Recognition failed. Check the API key, connection, and model configuration.";
            MessageBox.Show(
                ex.Message,
                "Could not recognize the drawing",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isAnalyzing = false;
            SuggestButton.IsEnabled = true;
        }
    }

    private void SuggestionOverlay_CandidateSelected(object? sender, SuggestionCandidateEventArgs e)
    {
        if (_roughStrokes.Count == 0)
        {
            SuggestionOverlay.Dismiss();
            StatusText.Text = "Draw something before choosing a suggestion.";
            return;
        }

        Rect bounds = GetRoughStrokeBounds();
        foreach (Polyline stroke in _roughStrokes)
        {
            DrawingCanvas.Children.Remove(stroke);
        }
        _roughStrokes.Clear();

        FrameworkElement cleanElement = CreateCleanElement(e.Candidate, bounds.Size);
        double width = GetElementWidth(cleanElement);
        double height = GetElementHeight(cleanElement);
        Point center = new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        System.Windows.Controls.Canvas.SetLeft(cleanElement, center.X - width / 2);
        System.Windows.Controls.Canvas.SetTop(cleanElement, center.Y - height / 2);
        DrawingCanvas.Children.Add(cleanElement);

        SuggestionOverlay.Dismiss();
        StatusText.Text = $"Applied {e.Candidate.Label}. Draw the next item or clear the board.";
    }

    private void SuggestionOverlay_OverlayClosed(object? sender, EventArgs e)
    {
        StatusText.Text = "Suggestions closed. Your rough drawing is unchanged.";
    }

    private void ClearBoardButton_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.Children.Clear();
        _roughStrokes.Clear();
        _currentStroke = null;
        _isDrawing = false;
        SuggestionOverlay.Dismiss();
        StatusText.Text = "Board cleared. Draw a shape, letter, or digit.";
    }

    private byte[] CaptureRoughStrokesAsPng()
    {
        Rect bounds = GetRoughStrokeBounds(padding: 20);
        int pixelWidth = Math.Clamp((int)Math.Ceiling(bounds.Width), 80, 1600);
        int pixelHeight = Math.Clamp((int)Math.Ceiling(bounds.Height), 80, 1200);

        var drawingVisual = new DrawingVisual();
        using (DrawingContext context = drawingVisual.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, pixelWidth, pixelHeight));

            foreach (Polyline stroke in _roughStrokes)
            {
                if (stroke.Points.Count == 0)
                {
                    continue;
                }

                var pen = new Pen(Brushes.DarkSlateBlue, stroke.StrokeThickness)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };

                if (stroke.Points.Count == 1)
                {
                    Point point = stroke.Points[0];
                    context.DrawEllipse(
                        Brushes.DarkSlateBlue,
                        null,
                        new Point(point.X - bounds.Left, point.Y - bounds.Top),
                        stroke.StrokeThickness / 2,
                        stroke.StrokeThickness / 2);
                    continue;
                }

                var geometry = new StreamGeometry();
                using (StreamGeometryContext geometryContext = geometry.Open())
                {
                    Point first = Translate(stroke.Points[0], bounds);
                    geometryContext.BeginFigure(first, isFilled: false, isClosed: false);
                    List<Point> remaining = stroke.Points.Skip(1)
                        .Select(point => Translate(point, bounds))
                        .ToList();
                    geometryContext.PolyLineTo(remaining, isStroked: true, isSmoothJoin: true);
                }
                geometry.Freeze();
                context.DrawGeometry(null, pen, geometry);
            }
        }

        var bitmap = new RenderTargetBitmap(
            pixelWidth,
            pixelHeight,
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(drawingVisual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private Rect GetRoughStrokeBounds(double padding = 0)
    {
        Point[] points = _roughStrokes
            .Where(stroke => stroke.Points.Count > 0)
            .SelectMany(stroke => stroke.Points)
            .ToArray();

        if (points.Length == 0)
        {
            return new Rect(0, 0, 100, 100);
        }

        double minX = points.Min(point => point.X);
        double minY = points.Min(point => point.Y);
        double maxX = points.Max(point => point.X);
        double maxY = points.Max(point => point.Y);

        double strokePadding = padding + 8;
        double left = Math.Max(0, minX - strokePadding);
        double top = Math.Max(0, minY - strokePadding);
        double right = maxX + strokePadding;
        double bottom = maxY + strokePadding;

        return new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    private static Point Translate(Point point, Rect bounds) =>
        new(point.X - bounds.Left, point.Y - bounds.Top);

    private static FrameworkElement CreateCleanElement(SuggestionCandidate candidate, Size roughSize)
    {
        double width = Math.Clamp(roughSize.Width, 75, 210);
        double height = Math.Clamp(roughSize.Height, 70, 190);
        Brush stroke = Brushes.DodgerBlue;

        if (candidate.Category == DrawingCategory.Letter || candidate.Category == DrawingCategory.Digit)
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = candidate.CanonicalValue,
                Width = Math.Clamp(width, 70, 180),
                Height = Math.Clamp(height, 75, 175),
                FontSize = Math.Clamp(Math.Min(width, height) * 0.78, 42, 110),
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = stroke,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        if (candidate.Category != DrawingCategory.Shape)
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = candidate.Label,
                Width = width,
                Height = height,
                FontSize = 28,
                Foreground = stroke,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        string shape = candidate.CanonicalValue.Trim().ToLowerInvariant();
        switch (shape)
        {
            case "circle":
            {
                double diameter = Math.Clamp(Math.Max(width, height), 75, 180);
                return new Ellipse { Width = diameter, Height = diameter, Stroke = stroke, StrokeThickness = 4, Fill = Brushes.Transparent };
            }
            case "oval":
            case "ellipse":
                return new Ellipse { Width = Math.Clamp(width, 95, 210), Height = Math.Clamp(height, 65, 150), Stroke = stroke, StrokeThickness = 4, Fill = Brushes.Transparent };
            case "square":
            {
                double side = Math.Clamp(Math.Max(width, height), 75, 160);
                return new Rectangle { Width = side, Height = side, Stroke = stroke, StrokeThickness = 4, Fill = Brushes.Transparent };
            }
            case "rectangle":
                return new Rectangle { Width = Math.Clamp(width, 95, 210), Height = Math.Clamp(height, 65, 150), Stroke = stroke, StrokeThickness = 4, Fill = Brushes.Transparent };
            case "triangle":
                return CreatePolygon(width, height,
                    new Point(width / 2, 0), new Point(width, height), new Point(0, height));
            case "diamond":
                return CreatePolygon(width, height,
                    new Point(width / 2, 0), new Point(width, height / 2),
                    new Point(width / 2, height), new Point(0, height / 2));
            case "pentagon":
                return CreateRegularPolygon(width, height, 5);
            case "hexagon":
                return CreateRegularPolygon(width, height, 6);
            case "star":
                return CreateStar(width, height);
            default:
                return new System.Windows.Controls.TextBlock
                {
                    Text = candidate.CanonicalValue,
                    Width = width,
                    Height = height,
                    FontSize = 28,
                    Foreground = stroke,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
        }
    }

    private static Polygon CreatePolygon(double width, double height, params Point[] points) => new()
    {
        Width = width,
        Height = height,
        Points = new PointCollection(points),
        Stroke = Brushes.DodgerBlue,
        StrokeThickness = 4,
        Fill = Brushes.Transparent,
        Stretch = Stretch.Fill
    };

    private static Polygon CreateRegularPolygon(double width, double height, int sides)
    {
        var points = new List<Point>();
        for (int i = 0; i < sides; i++)
        {
            double angle = (Math.PI * 2 * i / sides) - Math.PI / 2;
            points.Add(new Point(
                width / 2 + Math.Cos(angle) * width * 0.48,
                height / 2 + Math.Sin(angle) * height * 0.48));
        }
        return CreatePolygon(width, height, points.ToArray());
    }

    private static Polygon CreateStar(double width, double height)
    {
        var points = new List<Point>();
        for (int i = 0; i < 10; i++)
        {
            double angle = (Math.PI * i / 5) - Math.PI / 2;
            double radius = i % 2 == 0 ? 0.48 : 0.22;
            points.Add(new Point(
                width / 2 + Math.Cos(angle) * width * radius,
                height / 2 + Math.Sin(angle) * height * radius));
        }
        return CreatePolygon(width, height, points.ToArray());
    }

    private static double GetElementWidth(FrameworkElement element) =>
        double.IsNaN(element.Width) ? element.DesiredSize.Width : element.Width;

    private static double GetElementHeight(FrameworkElement element) =>
        double.IsNaN(element.Height) ? element.DesiredSize.Height : element.Height;
}
