using Cloud.Crash;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests the lookup of module names from assembly names.
/// </summary>
[TestClass]
public class TestModuleCatalog
{
    /// <summary>
    /// Known assemblies are found, ignoring case and version suffixes.
    /// </summary>
    [TestMethod]
    public void KnownAssemblyIsFound()
    {
        Assert.IsTrue(ModuleCatalog.TryGetModule("Chat", out string module));
        Assert.AreEqual("Chat", module);

        Assert.IsTrue(ModuleCatalog.TryGetModule("incident", out module));
        Assert.AreEqual("Incidents", module);

        Assert.IsTrue(ModuleCatalog.TryGetModule("FileSynchroniser, Version=1.0.0.0, Culture=neutral", out module));
        Assert.AreEqual("File Sync", module);
    }

    /// <summary>
    /// Framework, third-party, blank and null assemblies are not modules.
    /// </summary>
    [TestMethod]
    public void UnknownAssemblyIsNotFound()
    {
        Assert.IsFalse(ModuleCatalog.TryGetModule("System.Private.CoreLib", out string module));
        Assert.AreEqual(ModuleCatalog.UnknownModule, module);
        Assert.IsFalse(ModuleCatalog.TryGetModule(" ", out _));
        Assert.IsFalse(ModuleCatalog.TryGetModule(null, out _));
    }
}
