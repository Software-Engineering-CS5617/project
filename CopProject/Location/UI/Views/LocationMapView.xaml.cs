using System.Text.Json;
using LocationTracker.Interfaces;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LocationTracker.Views;

public sealed partial class LocationMapView : UserControl
{
    private IMapService? _map;

    public LocationMapView()
    {
        InitializeComponent();
    }

    public async void Initialize(IMapService map)
    {
        _map = map;
        MapView.NavigationCompleted += MapView_NavigationCompleted;
        // CoreWebView2 is initialized asynchronously; wait for it before navigating,
        // otherwise NavigateToString throws "A method was called at an unexpected time."
        await MapView.EnsureCoreWebView2Async();

        // OpenStreetMap blocks tile requests without a descriptive User-Agent.
        MapView.CoreWebView2.Settings.UserAgent =
            "CopProject-LocationTracker/1.0 (https://github.com/Software-Engineering-CS5617/project)";

        MapView.NavigateToString(MapPage);
    }

    private async void MapView_NavigationCompleted(
        WebView2 sender,
        CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess || _map is null)
        {
            return;
        }

        string incidents = JsonSerializer.Serialize(
            _map.GetIncidentsForMap().Select(incident => new
            {
                incident.Title,
                incident.Description,
                Category = incident.Category.ToString(),
                Severity = incident.Severity.ToString(),
                Status = incident.Status.ToString(),
                Latitude = incident.Location.Latitude,
                Longitude = incident.Location.Longitude,
                incident.Location.Address
            }));

        await MapView.ExecuteScriptAsync($"renderIncidents({incidents});");
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
            html, body, #map { height: 100%; margin: 0; }
          </style>
        </head>
        <body>
          <div id="map"></div>
          <script>
            const map = L.map('map').setView([10.79, 76.68], 12);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
              attribution: '&copy; OpenStreetMap contributors'
            }).addTo(map);

            function renderIncidents(incidents) {
              incidents.forEach(incident => {
                const popup = `<strong>${incident.Title}</strong><br>
                  ${incident.Category} · ${incident.Severity}<br>
                  ${incident.Address ?? ''}<br>${incident.Description}`;
                L.marker([incident.Latitude, incident.Longitude])
                  .addTo(map)
                  .bindPopup(popup);
              });
            }
          </script>
        </body>
        </html>
        """;
}
