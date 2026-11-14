using System.Globalization;
using Incidents.ViewModels;
using Incidents.Views;
using LocationTracker.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace LocationTracker.Views;

/// <summary>
/// Small map that shows a pin at the latitude and longitude entered on the Create Incident page.
/// </summary>
public sealed partial class MiniMapView : UserControl
{
    public event Action<double, double>? LocationSelected;

    private bool _mapInitializationStarted;
    private bool _isMapReady;
    private double _latitude = double.NaN;
    private double _longitude = double.NaN;

    public MiniMapView()
    {
        InitializeComponent();
        Loaded += MiniMapView_Loaded;
    }

    /// <summary>
    /// Puts a mini map inside the Create Incident page and moves its pin
    /// whenever latitude or longitude changes on that page.
    /// </summary>
    public static void AttachTo(CreateIncidentPage page)
    {
        page.Loaded += async (sender, e) => {
            CreateIncidentPageViewModel? viewModel = page.ViewModel;
            if (viewModel is null || page.MiniMapHost.Content is not null)
            {
                return;
            }

            MiniMapView miniMap = new();
            page.MiniMapHost.Content = miniMap;
            miniMap.LocationSelected += (latitude, longitude) => {
                viewModel.Latitude = latitude;
                viewModel.Longitude = longitude;
            };

            viewModel.PropertyChanged += async (s, args) => {
                if (args.PropertyName == nameof(viewModel.Latitude) ||
                    args.PropertyName == nameof(viewModel.Longitude))
                {
                    await miniMap.ShowLocationAsync(viewModel.Latitude, viewModel.Longitude);
                }
            };

            // location is already filled in when we come from the location tracker map
            if (viewModel.Latitude != 0 || viewModel.Longitude != 0)
            {
                await miniMap.ShowLocationAsync(viewModel.Latitude, viewModel.Longitude);
            }
        };
    }

    /// <summary>
    /// Moves the pin to the given location. If the map has not loaded yet
    /// the location is kept and shown once it is ready.
    /// </summary>
    public async Task ShowLocationAsync(double latitude, double longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
        await UpdatePinAsync();
    }

    private async void MiniMapView_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeMapAsync();
    }

    /// <summary>Initializes WebView2 before loading the map.</summary>
    private async Task InitializeMapAsync()
    {
        if (_mapInitializationStarted || MapView.CoreWebView2 is not null)
        {
            return;
        }

        _mapInitializationStarted = true;
        MapView.NavigationCompleted += MapView_NavigationCompleted;
        await MapView.EnsureCoreWebView2Async();

        CoreWebView2 webView = MapView.CoreWebView2
            ?? throw new InvalidOperationException("WebView2 failed to initialize.");
        webView.Settings.UserAgent =
            "CopProject-LocationTracker/1.0 (https://github.com/Software-Engineering-CS5617/project)";
        webView.WebMessageReceived += WebMessageReceived;
        MapView.NavigateToString(MapPage);
    }

    private void WebMessageReceived(
        CoreWebView2 sender,
        CoreWebView2WebMessageReceivedEventArgs args)
    {
        string[] values = args.TryGetWebMessageAsString().Split(',');
        if (values.Length != 2 ||
            !double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude) ||
            !double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude) ||
            !Coordinates.IsValid(latitude, longitude))
        {
            return;
        }

        LocationSelected?.Invoke(latitude, longitude);
    }

    private async void MapView_NavigationCompleted(
        WebView2 sender,
        CoreWebView2NavigationCompletedEventArgs args)
    {
        _isMapReady = args.IsSuccess;
        await UpdatePinAsync();
    }

    private async Task UpdatePinAsync()
    {
        if (!_isMapReady || !Coordinates.IsValid(_latitude, _longitude))
        {
            return;
        }

        string lat = _latitude.ToString(CultureInfo.InvariantCulture);
        string lng = _longitude.ToString(CultureInfo.InvariantCulture);
        await MapView.ExecuteScriptAsync($"showLocation({lat}, {lng});");
    }

    private const string MapPage = """
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <link rel="stylesheet"
                href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
          <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
          <style>
            html, body, #map { height: 100%; width: 100%; margin: 0; }
          </style>
        </head>
        <body>
          <div id="map"></div>
          <script>
            const map = L.map('map').setView([10.79, 76.68], 12);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
              attribution: '&copy; OpenStreetMap contributors'
            }).addTo(map);

            let marker = null;

            map.on('click', e => {
              window.chrome.webview.postMessage(
                e.latlng.lat + ',' + e.latlng.lng);
            });

            function showLocation(lat, lng) {
              if (marker) {
                marker.setLatLng([lat, lng]);
              } else {
                marker = L.marker([lat, lng]).addTo(map);
              }
              map.setView([lat, lng], 15);
            }
          </script>
        </body>
        </html>
        """;
}
