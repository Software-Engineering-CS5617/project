using System;
using System.Collections.Generic;
using System.Text;

namespace Cloud.Persistence;

/// <summary>
/// This enum defines the different modules that can use the persistence layer.
/// This helps the factory to map the module to the correct storage container
/// </summary>

public enum ModuleType
{
    /// <summary>The chat module</summary>
    Chat,

    /// <summary>The whiteboard module</summary>
    WhiteBoard,

    /// <summary>The networking module</summary>
    Networking,

    /// <summary>The file synchronization module</summary>
    FileSynchronizer,

    /// <summary>The screen sharing module</summary>
    ScreenShare,

    /// <summary>The location tracking module.</summary>
    LocationTracker,

    /// <summary>The incident management module.</summary>
    IncidentManagement,

    /// <summary>The UX module.</summary>
    UX,

    ///<summary>The insights module</summary>
    Insights,

    ///<summary>The Controller module</summary>
    Controller


}
