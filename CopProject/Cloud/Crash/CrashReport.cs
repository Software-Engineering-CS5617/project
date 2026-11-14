using System.Xml.Serialization;

namespace Cloud.Crash;

/// <summary>
/// A single captured crash, serialized to XML for local storage and for upload.
/// </summary>
[XmlRoot("CrashReport")]
public sealed class CrashReport
{
    /// <summary>
    /// The schema version written by this build.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Gets or sets the version of the XML schema this report was written with.
    /// </summary>
    [XmlAttribute("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets the unique ID of the report.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets when the crash happened. XML round-trips give a local-kind value, so call ToUniversalTime before comparing.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the process was terminating because of this exception.
    /// </summary>
    public bool IsFatal { get; set; }

    /// <summary>
    /// Gets or sets the hook that caught the exception, for example "AppDomain" or "WinUI".
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the application.
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the operating system.
    /// </summary>
    public string OsDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user who hit the crash, if known.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the exception chain, outermost exception first and root cause last.
    /// </summary>
    [XmlArray("Exceptions")]
    [XmlArrayItem("Exception")]
    public List<CrashExceptionInfo> Exceptions { get; set; } = [];

    /// <summary>
    /// Gets or sets the complete exception text (<c>Exception.ToString()</c>) for display.
    /// </summary>
    public string FullText { get; set; } = string.Empty;
}
