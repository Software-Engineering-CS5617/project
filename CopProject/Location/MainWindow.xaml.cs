using Incidents;
using LocationTracker.Services;
using Microsoft.UI.Xaml;

namespace LocationTracker;

public sealed partial class MainWindow : Window
{
    private readonly MapService _map;

    public MainWindow()
    {
        InitializeComponent();

        var incidents = new IncidentController();
        var geocoding = new NominatimGeocodingService(new HttpClient());
        _map = new MapService(incidents, geocoding);

        var map = new LocationTracker.Views.LocationMapView();
        RootContent.Content = map;
        map.Initialize(_map);
    }
}
