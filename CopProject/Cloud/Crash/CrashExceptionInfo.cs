using System.Xml.Serialization;

namespace Cloud.Crash;

/// <summary>
/// One exception in the chain of a captured crash.
/// </summary>
public sealed class CrashExceptionInfo
{
    /// <summary>
    /// Gets or sets the full type name of the exception.
    /// </summary>
    [XmlAttribute("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stack frames, with the most recent call first.
    /// </summary>
    [XmlArray("Frames")]
    [XmlArrayItem("Frame")]
    public List<CrashFrame> Frames { get; set; } = [];
}
