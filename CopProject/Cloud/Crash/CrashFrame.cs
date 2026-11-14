using System.Xml.Serialization;

namespace Cloud.Crash;

/// <summary>
/// One stack frame of a captured exception.
/// </summary>
public sealed class CrashFrame
{
    /// <summary>
    /// Gets or sets the full name of the (open generic) type that declares the method, including nested types.
    /// </summary>
    [XmlAttribute("type")]
    public string DeclaringType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name as reported by the runtime.
    /// </summary>
    [XmlAttribute("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the simple name of the assembly that contains the method.
    /// </summary>
    [XmlAttribute("assembly")]
    public string Assembly { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source file name (without directories), if debug information is available.
    /// </summary>
    [XmlAttribute("file")]
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the source line number, or 0 when unknown.
    /// </summary>
    [XmlAttribute("line")]
    public int Line { get; set; }
}
