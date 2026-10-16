
using System.Xml.Serialization;

namespace Incidents;

internal static class IncidentSerializer
{
    private static readonly XmlSerializer s_serializer =
        new(typeof(IncidentXmlDocument));

    public static byte[] Serialize(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        IncidentXmlDocument document = new() {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            CreatedBy = incident.CreatedBy,
            Location = new IncidentLocationXml {
                Latitude = incident.Location.Latitude,
                Longitude = incident.Location.Longitude,
                Address = incident.Location.Address
            },
            Status = incident.Status,
            Category = incident.Category,
            Severity = incident.Severity,
            CreationTime = incident.CreationTime,
            UpdateTime = incident.UpdateTime
        };

        foreach (IncidentEvidence evidence in incident.Evidence)
        {
            document.Evidence.Add(new IncidentEvidenceXml {
                Id = evidence.Id,
                Description = evidence.Description,
                FilePath = evidence.FilePath,
                UploadTime = evidence.UploadTime,
                UploadedBy = evidence.UploadedBy
            });
        }

        foreach (IChange change in incident.ChangeHistory)
        {
            document.Changes.Add(new ChangeXmlDocument {
                Description = change.Description,
                ChangedTime = change.ChangedTime,
                ChangedBy = change.ChangedBy
            });
        }

        foreach (Guid edge in incident.AdjacentIncidentIds)
        {
            document.Edges.Add(edge);
        }

        using MemoryStream stream = new();
        s_serializer.Serialize(stream, document);

        return stream.ToArray();
    }

    public static Incident Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using MemoryStream stream = new(data);

        IncidentXmlDocument document =
            s_serializer.Deserialize(stream) as IncidentXmlDocument
            ?? throw new InvalidOperationException(
                "Could not deserialize incident XML.");

        List<IncidentEvidence> evidence = new();

        foreach (IncidentEvidenceXml item in document.Evidence)
        {
            evidence.Add(new IncidentEvidence(
                item.Id,
                item.Description,
                item.FilePath,
                item.UploadTime,
                item.UploadedBy));
        }

        List<IChange> changes = new();

        foreach (ChangeXmlDocument item in document.Changes)
        {
            changes.Add(new IncidentChange(
                item.Description,
                item.ChangedTime,
                item.ChangedBy));
        }

        return new Incident(
            document.Id,
            document.Title,
            document.Description,
            document.CreatedBy,
            new IncidentLocation(
                document.Location.Latitude,
                document.Location.Longitude,
                document.Location.Address),
            document.Status,
            document.Category,
            document.Severity,
            document.CreationTime,
            document.UpdateTime,
            evidence,
            changes,
            document.Edges);
    }

    private sealed record IncidentChange(
        string Description,
        DateTime ChangedTime,
        Guid ChangedBy) : IChange;
}

[XmlRoot("Incident")]
public class IncidentXmlDocument
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid CreatedBy { get; set; }

    public IncidentLocationXml Location { get; set; } = new();

    public IncidentStatus Status { get; set; }
    public IncidentCategory Category { get; set; }
    public IncidentSeverity Severity { get; set; }

    public DateTime CreationTime { get; set; }
    public DateTime UpdateTime { get; set; }

    [XmlArray("Evidence")]
    [XmlArrayItem("Item")]
    public List<IncidentEvidenceXml> Evidence { get; set; } = new();

    [XmlArray("Changes")]
    [XmlArrayItem("Change")]
    public List<ChangeXmlDocument> Changes { get; set; } = new();

    [XmlArray("Edges")]
    [XmlArrayItem("IncidentId")]
    public List<Guid> Edges { get; set; } = new();
}

public class IncidentLocationXml
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }

    public IncidentLocationXml() { }
}

public class IncidentEvidenceXml
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public string? FilePath { get; set; }
    public DateTime UploadTime { get; set; }
    public Guid UploadedBy { get; set; }

    public IncidentEvidenceXml() { }
}

public class ChangeXmlDocument
{
    public string Description { get; set; } = "";
    public DateTime ChangedTime { get; set; }
    public Guid ChangedBy { get; set; }

    public ChangeXmlDocument() { }
}
