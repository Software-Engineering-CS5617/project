namespace ScreenShare;

/// <summary>
/// Values for interacting with Networking module
/// </summary>

public static class ScreenShareModule
{
    public const int ModuleId = 8;

    public const int ControlPriority = 3;

    public const int DataPriority = 1;

    public static int PriorityFor(IScreenShareMessage message)
    {
        return message.Kind == ScreenShareMessageKind.Data ? DataPriority : ControlPriority;
    }
}
