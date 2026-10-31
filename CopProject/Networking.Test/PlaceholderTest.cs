using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Queues;

namespace Networking.Test;

/// <summary>
/// Placeholder test verifying that the Networking module assembly is referenced.
/// </summary>
[TestClass]
public class PlaceholderTest
{
    /// <summary>
    /// Verifies that Networking types are accessible to the test runner.
    /// </summary>
    [TestMethod]
    public void Networking_Assembly_Is_Referenced()
    {
        Assert.IsNotNull(typeof(Queue));
    }
}
