namespace Cloud.Crash;

/// <summary>
/// Maps assembly names of the project's own modules to module display names.
/// </summary>
public static class ModuleCatalog
{
    /// <summary>
    /// The module name used when no application frame is available.
    /// </summary>
    public const string UnknownModule = "Unknown";

    private static readonly Dictionary<string, string> s_modulesByAssembly = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Auth"] = "Authentication",
        ["Chat"] = "Chat",
        ["Cloud"] = "Cloud",
        ["Controller"] = "Controller",
        ["FileSynchroniser"] = "File Sync",
        ["Incident"] = "Incidents",
        ["Insights"] = "Insights",
        ["Location"] = "Location",
        ["Networking"] = "Networking",
        ["Screenshare"] = "Screen Share",
        ["UX"] = "UX",
        ["Whiteboard"] = "Whiteboard"
    };

    /// <summary>
    /// Looks up the module that owns an assembly.
    /// </summary>
    /// <param name="assemblyName">A simple assembly name, or a full name such as "Chat, Version=1.0.0.0".</param>
    /// <param name="module">Receives the module display name when found.</param>
    /// <returns><see langword="true"/> if the assembly belongs to one of the project's modules.</returns>
    public static bool TryGetModule(string? assemblyName, out string module)
    {
        module = UnknownModule;

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return false;
        }

        int comma = assemblyName.IndexOf(',', StringComparison.Ordinal);
        string simpleName = (comma < 0 ? assemblyName : assemblyName[..comma]).Trim();

        if (s_modulesByAssembly.TryGetValue(simpleName, out string? found))
        {
            module = found;
            return true;
        }

        return false;
    }
}
