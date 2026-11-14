using System.Text.Json;
using Incidents;
using LocationTracker.Interfaces;
using LocationTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace LocationTracker.Views;

public sealed partial class LocationMapView : UserControl
{
    public event Action<Guid>? IncidentSelected;
    public event Action<IncidentLocation>? LocationSelected;

    private readonly IMapService _map;
    private readonly LocationMapViewModel _viewModel;
    private bool _mapInitializationStarted;

    public LocationMapView(IMapService map)
    {
        InitializeComponent();
        _map = map;
        _viewModel = new LocationMapViewModel(map);
        _viewModel.IncidentsChanged += ViewModel_IncidentsChanged;
        DataContext = _viewModel;
        Loaded += LocationMapView_Loaded;
    }

    private async void LocationMapView_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeMapAsync();
    }

    /// <summary>Initializes WebView2 before loading the map.</summary>
    /// <remarks>OpenStreetMap requires a descriptive User-Agent for tile requests.</remarks>
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
        webView.WebMessageReceived += WebView_WebMessageReceived;

        MapView.NavigateToString(MapPage);
    }

    /// <summary>
    /// Handles messages received from the WebView2 control, such as map clicks and incident selections.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void WebView_WebMessageReceived(
        CoreWebView2 sender,
        CoreWebView2WebMessageReceivedEventArgs args)
    {
        string messageJson = args.TryGetWebMessageAsString()
            ?? args.WebMessageAsJson;
        using JsonDocument message = JsonDocument.Parse(messageJson);
        if (!message.RootElement.TryGetProperty("type", out JsonElement type))
        {
            return;
        }

        if (type.GetString() == "map-click")
        {
            double latitude = message.RootElement.GetProperty("lat").GetDouble();
            double longitude = message.RootElement.GetProperty("lng").GetDouble();
            await OnMapClickedAsync(latitude, longitude);
            return;
        }

        if (type.GetString() != "incident-click" ||
            !message.RootElement.TryGetProperty("id", out JsonElement id) ||
            !Guid.TryParse(id.GetString(), out Guid incidentId))
        {
            return;
        }

        IncidentSelected?.Invoke(incidentId);
    }

    private async void ViewModel_IncidentsChanged(object? sender, EventArgs e)
    {
        if (MapView.CoreWebView2 is null)
        {
            return;
        }

        string incidents = JsonSerializer.Serialize(_viewModel.Incidents);
        await MapView.ExecuteScriptAsync($"setIncidents({incidents});");
    }

    /// <summary>
    /// Handles a click on the map by retrieving the location and invoking the LocationSelected event.
    /// </summary>
    private async Task OnMapClickedAsync(double latitude, double longitude)
    {
        IncidentLocation? location;
        try
        {
            location = await _map.GetLocationForClickAsync(latitude, longitude);
        }
        catch (HttpRequestException)
        {
            location = new IncidentLocation(latitude, longitude);
        }

        if (location is not null)
        {
            LocationSelected?.Invoke(location);
        }
    }

    private async void MapView_NavigationCompleted(
        WebView2 sender,
        CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess)
        {
            return;
        }

        await _viewModel.LoadAsync();
    }

    private const string MapPage = """
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <link rel="stylesheet"
                href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
          <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
          <style>
            html, body, #map { height: 100%; width: 100%; margin: 0; }
            body { background: #dce8f2; }
          </style>
        </head>
        <body>
          <div id="map"></div>
          <script>
            const map = L.map('map').setView([10.79, 76.68], 12);
            const incidentMarkers = L.layerGroup().addTo(map);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
              attribution: '&copy; OpenStreetMap contributors'
            }).addTo(map);

            function setIncidents(incidents) {
              incidentMarkers.clearLayers();
              incidents.forEach(incident => {
                const popup = `<button onclick="selectIncident('${incident.Id}')">
                    <strong>${incident.Title}</strong></button><br>
                  ${incident.Category} · ${incident.Severity}<br>
                  ${incident.Address ?? ''}<br>${incident.Description}`;
                L.marker([incident.Latitude, incident.Longitude])
                  .addTo(incidentMarkers)
                  .bindPopup(popup);
              });
            }

            map.on('click', e => {
              window.chrome.webview.postMessage(JSON.stringify({
                type: 'map-click',
                lat: e.latlng.lat,
                lng: e.latlng.lng
              }));
            });

            function selectIncident(id) {
              window.chrome.webview.postMessage(JSON.stringify({
                type: 'incident-click',
                id: id
              }));
            }
          </script>
        </body>
        </html>
        """;
}
