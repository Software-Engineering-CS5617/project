using System.Text.Json;

namespace Incidents;

internal static class IncidentSerializer
{
    private static readonly JsonSerializerOptions s_options =
        new(JsonSerializerDefaults.Web);

    public static byte[] Serialize(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        IncidentDocument document = new IncidentDocument {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            CreatedBy = incident.CreatedBy,
            Location = incident.Location,
            Status = incident.Status,
            Category = incident.Category,
            Severity = incident.Severity,
            CreationTime = incident.CreationTime,
            UpdateTime = incident.UpdateTime,

            Evidence = incident.Evidence.ToList(),

            Changes = incident.ChangeHistory
                .Select(change => new ChangeDocument {
                    Description = change.Description,
                    ChangedTime = change.ChangedTime,
                    ChangedBy = change.ChangedBy
                })
                .ToList(),

            Edges = incident.AdjacentIncidentIds.ToList()
        };

        return JsonSerializer.SerializeToUtf8Bytes(
            document,
            s_options);
    }

    public static Incident Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        IncidentDocument document =
            JsonSerializer.Deserialize<IncidentDocument>(data, s_options)
            ?? throw new JsonException(
                "Could not deserialize incident.");

        List<IChange> changes = document.Changes
            .Select(change => (IChange)new IncidentChange(
                change.Description,
                change.ChangedTime,
                change.ChangedBy))
            .ToList();

        return new Incident(
            document.Id,
            document.Title,
            document.Description,
            document.CreatedBy,
            document.Location,
            document.Status,
            document.Category,
            document.Severity,
            document.CreationTime,
            document.UpdateTime,
            document.Evidence,
            changes,
            document.Edges);
    }

    private sealed class IncidentDocument
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Guid CreatedBy { get; set; }

        public IncidentLocation Location { get; set; } = new(0, 0);

        public IncidentStatus Status { get; set; }
        public IncidentCategory Category { get; set; }
        public IncidentSeverity Severity { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime UpdateTime { get; set; }

        public List<IncidentEvidence> Evidence { get; set; } = [];
        public List<ChangeDocument> Changes { get; set; } = [];

        // Direct adjacency list, not the BFS result.
        public List<Guid> Edges { get; set; } = [];

        public IncidentDocument() { }
    }

    private sealed class ChangeDocument
    {
        public string Description { get; set; } = "";
        public DateTime ChangedTime { get; set; }
        public Guid ChangedBy { get; set; }

        public ChangeDocument() { }
    }

    private sealed record IncidentChange(
        string Description,
        DateTime ChangedTime,
        Guid ChangedBy
    ) : IChange;
}
