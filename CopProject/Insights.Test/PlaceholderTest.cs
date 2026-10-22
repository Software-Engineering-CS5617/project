using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Insights.Test;

/// <summary>
/// Placeholder test so that the Insights module is measured by the code coverage
/// pipeline even though it has no real tests yet. Referencing the module makes
/// the assembly part of the test run, so it is reported at 0% coverage instead
/// of being left out of the report entirely.
/// </summary>
[TestClass]
public class PlaceholderTest
{
    [TestMethod]
    public void Insights_Assembly_Is_Referenced()
    {
        Assembly? module = Assembly.Load("Insights");
        Assert.IsNotNull(module);
    }
}
