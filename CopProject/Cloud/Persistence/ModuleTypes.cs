using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence
{
    // This enum defines the different modules that can use the persistence layer.
    // This helps the factory to map the module to the correct storage container
    public enum ModuleType
    {
        Chat,
        WhiteBoard,
        Networking,

        FileSynchronizer,
        ScreenShare,
        LocationTracker,
        IncidentManagement,
        UX

        // we can add more modules here


    }
    
    
}
