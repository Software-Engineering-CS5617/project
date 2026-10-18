# Location Tracker: Specs

## Team Members
- Vishesh Srivastava 112301037
- Ankit Gupta 112301010

## 1. Goals

- One central map that shows incidents.
- Click an incident pin to see more information about it.
- Click on the map to add incidents
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
public enum IncidentSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum IncidentStatus
{
    Reported,
    InProgress,
    Resolved
}

// The main thing we store and show on the map
public class Incident
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";     
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
	public Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ReportedAt { get; set; }
}

// What the browser sends when reporting a new incident
public class CreateIncidentRequest
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public IncidentSeverity Severity { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class IncidentFilter
{
    public IncidentStatus? Status { get; set; }
    public string? Category { get; set; }
    public DateTime? Since { get; set; }
}
```

## 4. Interfaces

```csharp

public interface IIncidentRepository
{
    List<Incident> GetAll(IncidentFilter filter);
    Incident? GetById(Guid id);
    void Add(Incident incident);
    bool UpdateStatus(Guid id, IncidentStatus newStatus);   
}

// The main logic. The controller only talks to this.
public interface IIncidentService
{
    Task<Incident> CreateAsync(CreateIncidentRequest request);
    List<Incident> GetAllForMap(IncidentFilter filter);
    Incident? GetById(Guid id);
    Task<bool> UpdateStatusAsync(Guid id, IncidentStatus newStatus);
}

// Pushes live updates to everyone who has the map open.
public interface IIncidentNotifier
{
    Task NotifyIncidentCreatedAsync(Incident incident);
    Task NotifyIncidentUpdatedAsync(Incident incident);
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

