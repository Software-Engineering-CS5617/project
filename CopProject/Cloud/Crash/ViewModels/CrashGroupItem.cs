using System.Globalization;

namespace Cloud.Crash.ViewModels;

/// <summary>
/// A crash group prepared for display in a list.
/// </summary>
public sealed class CrashGroupItem
{
    /// <summary>
    /// Creates the item.
    /// </summary>
    /// <param name="group">The group to display.</param>
    public CrashGroupItem(CrashGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        Group = group;
    }

    /// <summary>
    /// Gets the underlying group.
    /// </summary>
    public CrashGroup Group { get; }

    /// <summary>
    /// Gets the fingerprint.
    /// </summary>
    public string Fingerprint => Group.Fingerprint;

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string CategoryText => Group.Category.ToString();

    /// <summary>
    /// Gets the module name.
    /// </summary>
    public string Module => Group.Module;

    /// <summary>
    /// Gets the full exception type name.
    /// </summary>
    public string ExceptionType => Group.ExceptionType;

    /// <summary>
    /// Gets the exception type name without its namespace.
    /// </summary>
    public string ShortExceptionType
    {
        get
        {
            int lastDot = Group.ExceptionType.LastIndexOf('.');
            return lastDot < 0 ? Group.ExceptionType : Group.ExceptionType[(lastDot + 1)..];
        }
    }

    /// <summary>
    /// Gets the normalized top frame.
    /// </summary>
    public string TopFrame => Group.TopFrame;

    /// <summary>
    /// Gets the sample message.
    /// </summary>
    public string SampleMessage => Group.SampleMessage;

    /// <summary>
    /// Gets the number of crashes in the group.
    /// </summary>
    public int Count => Group.Count;

    /// <summary>
    /// Gets when the latest crash happened, in local time.
    /// </summary>
    public string LastSeenText => Group.LastSeenUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}
