# Location Tracker: Specs

## Team Members
- Vishesh Srivastava 112301037
- Ankit Gupta 112301010

## 1. Goals

- One central map that shows incidents.
- Click an incident pin to see more information about it.
- Click on the map for a new incident, the incident itself is created through the Incident Management module.
- Filter on incidents on the basis of recency. 
- Search for an address for the map to jump there.

**Stretch goal:** whiteboard-style annotations on the map. 

## 2. Decisions for now

| Topic           | Decision                                     | Why                              |
| --------------- | -------------------------------------------- | -------------------------------- |
| Location format | Plain `double Latitude` / `double Longitude` | Simple no extra libraries needed |
| Live updates    | SignalR/Polling                              | free library                     |
| Map             | Leaflet + OpenStreetMap tiles                | Open source, free                |
| Address lookup  | Nominatim (OpenStreetMap)                    | Free, no API key                 |


## 3. Models

```csharp
# Models reused from Incident Management.
See `docs/incident_management/specification.md`.
- `Incident`: read-only for us; position is `Incident.Location`
- `IncidentLocation(double Latitude, double Longitude, string? Address)`
- `IncidentFilter`: we set `FromDateTime` for the recency filter
- `IncidentLocationFilter(Latitude, Longitude, Address?, Radius?)`: for "nearby" incidents.

# Our models
// Result of an address search, the map jumps to this point
public record LocationSearchResult(double Latitude, double Longitude, string? Address);

```

## 4. Interfaces

```csharp
// What the map/UI calls. Reads incidents through IIncidentController.
public interface IMapService
{
    List<Incident> GetIncidentsForMap(IncidentFilter? filter = null);
    Incident GetIncidentById(Guid id);
}

// Turns coordinates into an address (and the other way round).
public interface IGeocodingService
{
    Task<string?> GetAddressAsync(double latitude, double longitude);      
    Task<(double Latitude, double Longitude)?> FindCoordinatesAsync(string address);  
}
```

## 5. Stretch goal

Whiteboard-style annotations (circle an area, draw a line, add a text label). Rough idea for later:

- Frontend: Leaflet-Geoman lets users draw shapes on the map.
- Backend: a new IAnnotationService and IAnnotationRepository, kept separate from the incident code so the core is not affected.
- Live sharing: reuse SignalR with new events like `AnnotationAdded`.

