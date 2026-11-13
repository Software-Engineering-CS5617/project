using System;
using System.Diagnostics.CodeAnalysis;
using Incidents;

namespace Incidents.Views;

/// <summary>
/// Shared data passed between incident pages: the controller, the current user and the window handle.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record IncidentPageContext(
    IIncidentController IncidentController,
    string CurrentUserId,
    IntPtr WindowHandle
);

/// <summary>
/// Navigation parameters for opening the details page of one incident.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record IncidentDetailsPageParameters(
    IncidentPageContext Context,
    Guid IncidentId
);

/// <summary>
/// Navigation parameters for opening the Create Incident page with a location already filled in.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record IncidentCreatePageWithLocation(
    IncidentPageContext Context,
    double Latitude,
    double Longitude,
    string? Address
);
