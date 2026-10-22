using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Queues;

namespace Networking.Test;

/// <summary>
/// Placeholder test so that the Networking module is measured by the code
/// coverage pipeline even though it has no real tests yet. Referencing the
/// module makes the assembly part of the test run, so it is reported at 0%
/// coverage instead of being left out of the report entirely.
/// </summary>
[TestClass]
public class PlaceholderTest
{
    [TestMethod]
    public void Networking_Assembly_Is_Referenced()
    {
        Assert.IsNotNull(typeof(Queue));
    }
}
