using Cloud.Persistence;

namespace Cloud.Test;

/// <summary>
/// Tests the StorageFactory class. this is needed for better branch coverage
/// </summary>
[TestClass]
public class TestStorageFactory
{
    [TestMethod]
    ///<summary>
    /// tests module to container mapping for all supported modules.
    ///</summary>
    public void GetContainerNameMapsEverySupportedModule()
    {
        (ModuleType Module, string Container)[] mappings = [
            (ModuleType.Chat, "Chat"),
            (ModuleType.WhiteBoard, "Whiteboard"),
            (ModuleType.Networking, "Networking"),
            (ModuleType.FileSynchronizer, "FileSynchronizer"),
            (ModuleType.IncidentManagement, "IncidentManagement"),
            (ModuleType.Controller, "Controller"),
            (ModuleType.Insights, "Insights"),
            (ModuleType.LocationTracker, "LocationTracker"),
            (ModuleType.UX, "UX"),
            (ModuleType.ScreenShare, "ScreenShare"),
            (ModuleType.CrashGroups, "CrashGroups"),
            (ModuleType.CrashReports, "CrashReports")
        ];

        foreach ((ModuleType module, string expectedContainer) in mappings)
        {
            Assert.AreEqual(expectedContainer, StorageFactory.GetContainerName(module));
        }
    }

    /// <summary>
    /// tests that GetContainerName throws an exception for unsupported modules
    /// </summary>
    [TestMethod]
    public void GetContainerNameRejectsUnsupportedModule()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => StorageFactory.GetContainerName((ModuleType)int.MaxValue));
    }
}
