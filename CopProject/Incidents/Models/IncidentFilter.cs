using System;
using System.Text.RegularExpressions;

namespace Incidents;

public class IncidentFilter
{
    public string? Title { get; }
    public IncidentStatus? Status { get; }
    public IncidentCategory? Category { get; }
    public IncidentSeverity? Severity { get; }
    public DateTime? FromDateTime { get; }
    public DateTime? ToDateTime { get; }
    public Guid? CreatedBy { get; }
    public IncidentLocationFilter? Location { get; }

    public IncidentFilter(
        string? title,
        IncidentStatus? status,
        IncidentCategory? category,
        IncidentSeverity? severity,
        DateTime? fromDateTime,
        DateTime? toDateTime,
        Guid? createdBy,
        IncidentLocationFilter? location)
    {
        Title = title;
        Status = status;
        Category = category;
        Severity = severity;
        FromDateTime = fromDateTime;
        ToDateTime = toDateTime;
        CreatedBy = createdBy;
        Location = location;
    }

    public static bool Matches(Incident incident, IncidentFilter filter)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(filter);

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            bool titleMatches = Regex.IsMatch(
                incident.Title,
                filter.Title,
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));

            if (!titleMatches)
            {
                return false;
            }
        }

        if (filter.Status.HasValue && incident.Status != filter.Status.Value)
        {
            return false;
        }

        if (filter.Category.HasValue && incident.Category != filter.Category.Value)
        {
            return false;
        }

        if (filter.Severity.HasValue &&
            incident.Severity != filter.Severity.Value)
        {
            return false;
        }

        if (filter.FromDateTime.HasValue && incident.CreationTime < filter.FromDateTime.Value)
        {
            return false;
        }

        if (filter.ToDateTime.HasValue && incident.CreationTime > filter.ToDateTime.Value)
        {
            return false;
        }

        if (filter.CreatedBy.HasValue && incident.CreatedBy != filter.CreatedBy.Value)
        {
            return false;
        }

        if (filter.Location is not null && !IncidentLocationFilter.Matches(incident.Location, filter.Location))
        {
            return false;
        }

        return true;
    }
}
